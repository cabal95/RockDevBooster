using System;
using System.IO;
using System.Collections.Generic;

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.VisualStudio.Setup.Configuration;

namespace com.blueboxmoon.RockDevBooster
{
    /// <summary>
    /// Provides various helper methods used by the RockDevBooster application.
    /// </summary>
    static class Support
    {
        #region Visual Studio Related

        /// <summary>
        /// Get a list of all the visual studio instances installed on the system.
        /// </summary>
        /// <returns>A collection of VisualStudioInstall objects.</returns>
        static public List<VisualStudioInstall> GetVisualStudioInstances()
        {
            try
            {
                //
                // Read the first 10 VS install locations. Safe assumption they have less than 10.
                //
                var e = new SetupConfiguration().EnumInstances();
                var instances = new ISetupInstance[10];
                e.Next( 10, instances, out int fetched );

                var vsList = new List<VisualStudioInstall>();
                for ( int i = 0; i < fetched; i++ )
                {
                    vsList.Add( new VisualStudioInstall
                    {
                        Name = instances[i].GetDisplayName(),
                        Path = instances[i].GetInstallationPath()
                    } );
                }

                return vsList;
            }
            catch
            {
                return new List<VisualStudioInstall>();
            }
        }

        #endregion

        #region Data Path Methods

        /// <summary>
        /// Get the filesystem path to the RockDevBooster data folder.
        /// </summary>
        /// <returns>A string representing a location on the filesystem.</returns>
        static public string GetDataPath()
        {
            string appDataPath = Environment.GetEnvironmentVariable( "LocalAppData" );
            string oldDataPath = Path.Combine( appDataPath, "RockLauncher" );
            string dataPath = Path.Combine( appDataPath, "RockDevBooster" );

            if ( !Directory.Exists( dataPath ) )
            {
                if ( Directory.Exists( oldDataPath ) )
                {
                    Directory.Move( oldDataPath, dataPath );
                }
                else
                {
                    Directory.CreateDirectory( dataPath );
                }
            }

            return dataPath;
        }

        /// <summary>
        /// Get the path to the temporary package build directory.
        /// </summary>
        /// <returns>A string representing a location on the filesystem.</returns>
        static public string GetPackageBuildPath()
        {
            string dataPath = GetDataPath();
            string buildPath = Path.Combine( dataPath, "PackageBuild" );

            if ( !Directory.Exists( buildPath ) )
            {
                Directory.CreateDirectory( buildPath );
            }

            return buildPath;
        }

        /// <summary>
        /// Get the path to the temporary build directory.
        /// </summary>
        /// <returns>A string representing a location on the filesystem.</returns>
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

        /// <summary>
        /// Get the path to the Templates directory.
        /// </summary>
        /// <returns>A string representing a location on the filesystem.</returns>
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

        /// <summary>
        /// Get the path to the RockWeb instances.
        /// </summary>
        /// <returns>A string representing a location on the filesystem.</returns>
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

        #endregion

        #region Archival Methods

        /// <summary>
        /// Extract a ZIP archive onto the filesystem.
        /// </summary>
        /// <param name="archiveFilenameIn">The path to the ZIP file on disk.</param>
        /// <param name="outFolder">The directory to extract the files into.</param>
        /// <param name="progressCallback">An optional callback that is passed the percentage progress of the operation.</param>
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
                    //
                    // Process the progress callback.
                    //
                    count += 1;
                    if ( count % 10 == 0 )
                    {
                        progressCallback?.Invoke( count / ( double ) zf.Count );
                    }

                    //
                    // We only need to process files.
                    //
                    if ( !zipEntry.IsFile )
                    {
                        continue;
                    }

                    //
                    // Get the full on-disk path to the file and create the directory if needed.
                    //
                    String fullZipToPath = Path.Combine( outFolder, zipEntry.Name );
                    string directoryName = Path.GetDirectoryName( fullZipToPath );
                    if ( directoryName.Length > 0 )
                    {
                        Directory.CreateDirectory( directoryName );
                    }

                    //
                    // Read the compressed data and write the uncompressed data to disk.
                    //
                    byte[] buffer = new byte[4096];
                    Stream zipStream = zf.GetInputStream( zipEntry );
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

        /// <summary>
        /// Create a new ZIP file from the contents of a directory on disk.
        /// </summary>
        /// <param name="outPathname">The full path to the new ZIP file.</param>
        /// <param name="folderName">The directory to be compressed.</param>
        static public void CreateZipFromFolder( string outPathname, string folderName )
        {
            FileStream fsOut = File.Create( outPathname );
            ZipOutputStream zipStream = new ZipOutputStream( fsOut );

            zipStream.SetLevel( 3 );
            zipStream.Password = null;

            int folderOffset = folderName.Length + ( folderName.EndsWith( "\\" ) ? 0 : 1 );

            CompressFolder( folderName, zipStream, folderOffset );

            zipStream.IsStreamOwner = true;
            zipStream.Close();
        }

        /// <summary>
        /// Recursively called function to compress a directory and all it's contents.
        /// </summary>
        /// <param name="path">The path of the directory to compress.</param>
        /// <param name="zipStream">The Zip Stream to write the compressed files to.</param>
        /// <param name="folderOffset">How much of the path to strip when compressing files.</param>
        static private void CompressFolder( string path, ZipOutputStream zipStream, int folderOffset )
        {
            //
            // Process each file in the directory.
            //
            foreach ( string filename in Directory.GetFiles( path ) )
            {
                FileInfo fi = new FileInfo( filename );

                //
                // Get the relative filename and set the file information.
                //
                string entryName = filename.Substring( folderOffset );
                entryName = ZipEntry.CleanName( entryName );
                ZipEntry newEntry = new ZipEntry( entryName )
                {
                    DateTime = fi.LastWriteTime,
                    Size = fi.Length
                };

                //
                // Store the file in the archive.
                //
                byte[] buffer = new byte[4096];
                zipStream.PutNextEntry( newEntry );
                using ( FileStream streamReader = File.OpenRead( filename ) )
                {
                    StreamUtils.Copy( streamReader, zipStream, buffer );
                }
                zipStream.CloseEntry();
            }

            //
            // Process each sub-directory recursively.
            //
            foreach ( string folder in Directory.GetDirectories( path ) )
            {
                CompressFolder( folder, zipStream, folderOffset );
            }
        }

        #endregion
    }
}
