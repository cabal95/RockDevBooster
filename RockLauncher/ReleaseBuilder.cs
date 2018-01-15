using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace com.blueboxmoon.RockLauncher
{
    public class ReleaseBuilder
    {
        public event EventHandler StatusTextChanged;
        public string StatusText { get; private set; }

        public event EventHandler BuildCompleted;

        public string DevEnvExecutable { get; set; }

        protected string TemplateName { get; set; }

        private void UpdateStatusText( string text )
        {
            if ( StatusText != text )
            {
                StatusText = text;
                StatusTextChanged?.Invoke( this, new EventArgs() );
            }
        }

        public void DownloadRelease( string url, string template )
        {
            TemplateName = template;
            string filename = Path.Combine( Support.GetDataPath(), "temp.zip" );

            if ( false )
            {
                UnpackRelease( filename );
                return;
            }

            var webClient = new WebClient();

            webClient.Headers.Add( "User-Agent", "RockLauncher" );
            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
            webClient.DownloadFileAsync( new Uri( url ), filename );
        }

        private void UnpackRelease( string filename )
        {
            UpdateStatusText( "Unpacking..." );

            Directory.Delete( Support.GetBuildPath(), true );
            ExtractZipFile( filename, Support.GetBuildPath() );

            BuildRelease();
        }

        private void BuildRelease()
        {
            UpdateStatusText( "Restoring References" );

            if ( !NuGetRestore() )
            {
                BuildCompleted?.Invoke( this, new EventArgs() );
                return;
            }

            foreach ( var d in Directory.EnumerateDirectories( Support.GetBuildPath() ) )
            {
                CopyProjectReferences( d );
            }

            UpdateStatusText( "Building..." );

            var process = new System.Diagnostics.Process();
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized,
                FileName = DevEnvExecutable,
                Arguments = "Rock.sln /Build",
                WorkingDirectory = Support.GetBuildPath()
            };
            process.StartInfo = startInfo;

            process.Start();

            process.WaitForExit();
            if ( process.ExitCode != 0 )
            {
                UpdateStatusText( "Build Failed." );
                BuildCompleted?.Invoke( this, new EventArgs() );
                return;
            }

            UpdateStatusText( "Build Succeeded" );

            BuildTemplate();
        }

        protected void BuildTemplate()
        {
            UpdateStatusText( "Compressing RockWeb..." );

            var zipFile = Path.Combine( Support.GetTemplatesPath(), TemplateName + ".zip" );
            var rockWeb = Path.Combine( Support.GetBuildPath(), "RockWeb" );
            Support.CreateZipFromFolder( zipFile, rockWeb );

            UpdateStatusText( "Cleaning up..." );

            Directory.Delete( Support.GetBuildPath(), true );
            string tempfilename = Path.Combine( Support.GetDataPath(), "temp.zip" );
            File.Delete( tempfilename );

            UpdateStatusText( "Template has been created." );

            BuildCompleted?.Invoke( this, new EventArgs() );
        }

        private void CopyProjectReferences( string projectDirectory )
        {
            string destPath = Path.Combine( Path.Combine( Support.GetBuildPath(), "RockWeb" ), "Bin" );

            foreach ( var proj in Directory.EnumerateFiles( Path.Combine( Support.GetBuildPath(), projectDirectory ), "*.csproj" ) )
            {
                var doc = new XmlDocument();
                doc.Load( proj );
                var mgr = new XmlNamespaceManager( doc.NameTable );
                mgr.AddNamespace( "df", doc.DocumentElement.NamespaceURI );
                foreach ( XmlNode node in doc.DocumentElement.SelectNodes( "//df:HintPath", mgr ) )
                {
                    string dllFile = Path.Combine( Path.GetDirectoryName( proj ), node.InnerText );
                    string destFile = Path.Combine( destPath, Path.GetFileName( node.InnerText ) );
                    if ( File.Exists( dllFile ) && !File.Exists( destFile ) )
                    {
                        File.Copy( dllFile, Path.Combine( destPath, Path.GetFileName( node.InnerText ) ) );
                    }
                }
            }
        }

        private bool NuGetRestore()
        {
            var process = new System.Diagnostics.Process();
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized,
                FileName = Path.Combine( Environment.CurrentDirectory, "nuget.exe" ),
                Arguments = "restore",
                WorkingDirectory = Support.GetBuildPath()
            };
            process.StartInfo = startInfo;

            process.Start();

            process.WaitForExit();
            if ( process.ExitCode != 0 )
            {
                UpdateStatusText( "NuGet Restore Failed." );
                return false;
            }

            return true;
        }

        public void ExtractZipFile( string archiveFilenameIn, string outFolder )
        {
            ZipFile zf = null;
            try
            {
                FileStream fs = File.OpenRead( archiveFilenameIn );
                zf = new ZipFile( fs );

                string stripPath = string.Empty;
                foreach ( ZipEntry zipEntry in zf )
                {
                    if ( zipEntry.IsDirectory )
                    {
                        string name = zipEntry.Name;

                        name = name.Substring( 0, name.Length - 1 );

                        if ( Path.GetFileName( name ).Equals( "RockWeb", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            stripPath = name.Substring( 0, name.Length - 7 );
                        }
                    }
                }

                int count = 0;
                foreach ( ZipEntry zipEntry in zf )
                {
                    count += 1;
                    UpdateStatusText( string.Format( "Unpacking... {0}%", Math.Floor( count / ( double ) zf.Count * 100.0d ) ) );

                    if ( !zipEntry.IsFile )
                    {
                        continue;           // Ignore directories
                    }

                    String entryFileName = zipEntry.Name.Substring( stripPath.Length );

                    byte[] buffer = new byte[4096];     // 4K is optimum
                    Stream zipStream = zf.GetInputStream( zipEntry );

                    String fullZipToPath = Path.Combine( outFolder, entryFileName );
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

        private void WebClient_DownloadFileCompleted( object sender, System.ComponentModel.AsyncCompletedEventArgs e )
        {
            if ( e.Error != null )
            {
                UpdateStatusText( e.Error.Message );
                return;
            }

            string filename = System.IO.Path.Combine( Support.GetDataPath(), "temp.zip" );
            UnpackRelease( filename );
        }

        private void WebClient_DownloadProgressChanged( object sender, DownloadProgressChangedEventArgs e )
        {
            UpdateStatusText( string.Format( "Downloading {0:n0} MB...", e.BytesReceived / 1024.0d / 1024.0d ) );
        }

    }
}
