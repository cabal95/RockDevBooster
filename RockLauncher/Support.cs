using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.VisualStudio.Setup.Configuration;

namespace com.blueboxmoon.RockLauncher
{
    static class Support
    {
        static public List<VisualStudioInstall> GetVisualStudioInstances()
        {
            try
            {
                var e = new SetupConfiguration().EnumInstances();

                int fetched;
                var instances = new ISetupInstance[10];
                e.Next( 10, instances, out fetched );

                var vsList = new List<VisualStudioInstall>();
                for ( int i = 0; i < fetched; i++ )
                {
                    vsList.Add( new VisualStudioInstall { Name = instances[i].GetDisplayName(), Path = instances[i].GetInstallationPath() } );
                }

                return vsList;
            }
            catch
            {
                return new List<VisualStudioInstall>();
            }
        }

        static public string GetDataPath()
        {
            string appDataPath = Environment.GetEnvironmentVariable( "LocalAppData" );
            string dataPath = Path.Combine( appDataPath, "RockLauncher" );

            if ( !Directory.Exists( dataPath ) )
            {
                Directory.CreateDirectory( dataPath );
            }

            return dataPath;
        }

        static public string GetBuildPath()
        {
            string dataPath = GetDataPath();
            string buildPath = Path.Combine( dataPath, "Build" );

            if ( !Directory.Exists( buildPath ) )
            {
                Directory.CreateDirectory( buildPath );
            }

            return buildPath;
        }

        static public string GetTemplatesPath()
        {
            string dataPath = GetDataPath();
            string templatesPath = Path.Combine( dataPath, "Templates" );

            if ( !Directory.Exists( templatesPath ) )
            {
                Directory.CreateDirectory( templatesPath );
            }

            return templatesPath;
        }

        static public string GetInstancesPath()
        {
            string dataPath = GetDataPath();
            string instancesPath = Path.Combine( dataPath, "Instances" );

            if ( !Directory.Exists( instancesPath ) )
            {
                Directory.CreateDirectory( instancesPath );
            }

            return instancesPath;
        }

        static public void ExtractZipFile( string archiveFilenameIn, string outFolder, Action<double> progressCallback )
        {
            ZipFile zf = null;
            try
            {
                FileStream fs = File.OpenRead( archiveFilenameIn );
                zf = new ZipFile( fs );

                int count = 0;
                foreach ( ZipEntry zipEntry in zf )
                {
                    count += 1;
                    if ( count % 10 == 0 )
                    {
                        progressCallback?.Invoke( count / ( double ) zf.Count );
                    }

                    if ( !zipEntry.IsFile )
                    {
                        continue;
                    }

                    byte[] buffer = new byte[4096];
                    Stream zipStream = zf.GetInputStream( zipEntry );

                    String fullZipToPath = Path.Combine( outFolder, zipEntry.Name );
                    string directoryName = Path.GetDirectoryName( fullZipToPath );
                    if ( directoryName.Length > 0 )
                    {
                        Directory.CreateDirectory( directoryName );
                    }

                    using ( FileStream streamWriter = File.Create( fullZipToPath ) )
                    {
                        StreamUtils.Copy( zipStream, streamWriter, buffer );
                    }
                }
            }
            finally
            {
                if ( zf != null )
                {
                    zf.IsStreamOwner = true;
                    zf.Close();
                }
            }
        }

        static public void CreateZipFromFolder( string outPathname, string folderName )
        {
            FileStream fsOut = File.Create( outPathname );
            ZipOutputStream zipStream = new ZipOutputStream( fsOut );

            zipStream.SetLevel( 9 );
            zipStream.Password = null;

            int folderOffset = folderName.Length + ( folderName.EndsWith( "\\" ) ? 0 : 1 );

            CompressFolder( folderName, zipStream, folderOffset );

            zipStream.IsStreamOwner = true;
            zipStream.Close();
        }

        static private void CompressFolder( string path, ZipOutputStream zipStream, int folderOffset )
        {
            string[] files = Directory.GetFiles( path );

            foreach ( string filename in files )
            {
                FileInfo fi = new FileInfo( filename );

                string entryName = filename.Substring( folderOffset );
                entryName = ZipEntry.CleanName( entryName );
                ZipEntry newEntry = new ZipEntry( entryName )
                {
                    DateTime = fi.LastWriteTime,
                    Size = fi.Length
                };

                zipStream.PutNextEntry( newEntry );

                byte[] buffer = new byte[4096];
                using ( FileStream streamReader = File.OpenRead( filename ) )
                {
                    StreamUtils.Copy( streamReader, zipStream, buffer );
                }
                zipStream.CloseEntry();
            }

            string[] folders = Directory.GetDirectories( path );
            foreach ( string folder in folders )
            {
                CompressFolder( folder, zipStream, folderOffset );
            }
        }

    }

    public class VisualStudioInstall
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public string GetExecutable()
        {
            return FindExecutable( Path );
        }

        private string FindExecutable( string path )
        {
            string devenv = System.IO.Path.Combine( path, "devenv.com" );

            if ( File.Exists( devenv ) )
            {
                return devenv;
            }

            var dirs = Directory.GetDirectories( path );
            foreach ( var dir in dirs )
            {
                devenv = FindExecutable( dir );
                if ( devenv != null )
                {
                    return devenv;
                }
            }

            return null;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
