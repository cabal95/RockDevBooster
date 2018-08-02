using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace com.blueboxmoon.RockDevBooster.Builders
{
    public class ReleaseBuilder
    {
        #region Internal Fields

        /// <summary>
        /// The status text that we last knew. We track this so we don't send duplicate
        /// status updates.
        /// </summary>
        protected string StatusText { get; private set; }

        /// <summary>
        /// The name of the template to deploy this build as.
        /// </summary>
        protected string TemplateName { get; set; }

        /// <summary>
        /// The devenv.com executable to use when building this release.
        /// </summary>
        protected string DevEnvExecutable { get; set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Notification that the single-line status text has changed.
        /// </summary>
        public event EventHandler<string> StatusTextChanged;

        /// <summary>
        /// Notification that the stdout has new text to be displayed.
        /// </summary>
        public event EventHandler<string> ConsoleOutput;

        /// <summary>
        /// Notification that the build has completed.
        /// </summary>
        public event EventHandler BuildCompleted;

        #endregion

        #region Public Methods

        /// <summary>
        /// Download the tag-release from the GitHub URL and build it.
        /// </summary>
        /// <param name="url">The URL that contains the Zip archive of the release.</param>
        /// <param name="devenv">The Visual Studio executable to use when building.</param>
        /// <param name="template">What to name the template after it has been built.</param>
        public void DownloadRelease( string url, string devenv, string template )
        {
            TemplateName = template;
            DevEnvExecutable = devenv;

            string filename = Path.Combine( Support.GetDataPath(), "temp.zip" );

            UpdateStatusText( "Downloading..." );

            var webClient = new WebClient();

            webClient.Headers.Add( "User-Agent", "RockDevBooster" );
            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
            webClient.DownloadFileAsync( new Uri( url ), filename );
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Update the status text and notify the callback method.
        /// </summary>
        /// <param name="text">The new status text.</param>
        private void UpdateStatusText( string text )
        {
            if ( StatusText != text )
            {
                StatusText = text;
                StatusTextChanged?.Invoke( this, text );
            }
        }

        /// <summary>
        /// Unpack the ZIP file into the Build directory.
        /// </summary>
        /// <param name="filename">The full path to the ZIP file.</param>
        private void UnpackRelease( string filename )
        {
            UpdateStatusText( "Unpacking..." );

            Directory.Delete( Support.GetBuildPath(), true );
            ExtractZipFile( filename, Support.GetBuildPath() );

            BuildRelease();
        }

        /// <summary>
        /// Gets the referenced projects.
        /// </summary>
        /// <returns></returns>
        protected List<string> GetReferencedProjects()
        {
            var projectList = new List<string>();
            var solution = File.ReadAllText( Path.Combine( Support.GetBuildPath(), "Rock.sln" ) );

            var references = Regex.Match( solution, @"^\s*ProjectReferences\s*=\s*""(.*)""$", RegexOptions.Multiline );
            if ( references.Success )
            {
                var matches = Regex.Matches( references.Groups[1].Value, @"{[\w\-]+}" );
                foreach ( Match m in matches )
                {
                    var guidRef = m.Value;

                    var reference = Regex.Match( solution, @"Project\(""{[\w\-]+}""\)\s*=\s*""([^""]+)"",\s*""[^""]+"",\s*""" + guidRef + @"""", RegexOptions.IgnoreCase );

                    if ( reference.Success )
                    {
                        projectList.Add( reference.Groups[1].Value );
                    }
                }
            }

            return projectList;
        }

        /// <summary>
        /// Build the release so we have compiled DLLs.
        /// </summary>
        private void BuildRelease()
        {
            //
            // Restore any NuGet packages.
            //
            UpdateStatusText( "Restoring References" );
            if ( !NuGetRestore() )
            {
                BuildCompleted?.Invoke( this, new EventArgs() );
                return;
            }

            //
            // For some reason, MSBuild fails completely and the devenv build method
            // does not copy indirect DLL references (e.g. the NuGet DLLs) into the
            // RockWeb folder, so we need to do that manually.
            //
            foreach ( var d in GetReferencedProjects() )
            {
                CopyProjectReferences( d );
            }

            //
            // Launch a new devenv.com process to build the solution.
            //
            UpdateStatusText( "Building..." );
            var process = new Utilities.ConsoleApp( DevEnvExecutable );
            process.StandardTextReceived += Console_StandardTextReceived;
            process.WorkingDirectory = Support.GetBuildPath();
            process.ExecuteAsync( "Rock.sln", "/Build" );

            //
            // Wait for it to complete.
            //
            while ( process.Running )
            {
                Thread.Sleep( 100 );
            }

            //
            // Check if our build worked or not.
            //
            if ( process.ExitCode != 0 )
            {
                UpdateStatusText( "Build Failed." );
                BuildCompleted?.Invoke( this, new EventArgs() );
                return;
            }

            BuildTemplate();
        }

        /// <summary>
        /// Compress the RockWeb folder into a ZIP file as a template. Then do final cleanup.
        /// </summary>
        private void BuildTemplate()
        {
            //
            // Compress the RockWeb folder into a template ZIP file.
            //
            UpdateStatusText( "Compressing RockWeb..." );
            var zipFile = Path.Combine( Support.GetTemplatesPath(), TemplateName + ".zip" );
            var rockWeb = Path.Combine( Support.GetBuildPath(), "RockWeb" );
            Support.CreateZipFromFolder( zipFile, rockWeb );

            //
            // Cleanup temporary files.
            //
            UpdateStatusText( "Cleaning up..." );
            Directory.Delete( Support.GetBuildPath(), true );
            string tempfilename = Path.Combine( Support.GetDataPath(), "temp.zip" );
            File.Delete( tempfilename );

            UpdateStatusText( "Template has been created." );

            BuildCompleted?.Invoke( this, new EventArgs() );
        }

        /// <summary>
        /// Execute NuGet to restore all package references.
        /// </summary>
        /// <returns>True if the operation succeeded.</returns>
        private bool NuGetRestore()
        {
            //
            // Execute 'nuget.exe restore' in the solution directory.
            //
            var process = new Utilities.ConsoleApp( Path.Combine( Environment.CurrentDirectory, "nuget.exe" ) )
            {
                WorkingDirectory = Support.GetBuildPath()
            };
            process.StandardTextReceived += Console_StandardTextReceived;
            process.ExecuteAsync( "restore" );

            //
            // Wait for it to finish.
            //
            while (process.Running)
            {
                Thread.Sleep( 100 );
            }

            //
            // Make sure it worked.
            //
            if ( process.ExitCode != 0 )
            {
                UpdateStatusText( "NuGet Restore Failed." );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Copy all referenced DLL files from the project directory into the RockWeb/bin directory.
        /// We do this by scanning each csproj file in the directory and looking for HintPath nodes.
        /// </summary>
        /// <param name="projectDirectory">The project directory to scan.</param>
        private void CopyProjectReferences( string projectDirectory )
        {
            string destPath = Path.Combine( Path.Combine( Support.GetBuildPath(), "RockWeb" ), "Bin" );

            foreach ( var proj in Directory.EnumerateFiles( Path.Combine( Support.GetBuildPath(), projectDirectory ), "*.csproj" ) )
            {
                //
                // Load the csproj file.
                //
                var doc = new XmlDocument();
                doc.Load( proj );
                var mgr = new XmlNamespaceManager( doc.NameTable );
                mgr.AddNamespace( "df", doc.DocumentElement.NamespaceURI );

                //
                // Find and process all HintPath nodes.
                //
                foreach ( XmlNode node in doc.DocumentElement.SelectNodes( "//df:HintPath", mgr ) )
                {
                    string dllFile = Path.Combine( Path.GetDirectoryName( proj ), node.InnerText );
                    string destFile = Path.Combine( destPath, Path.GetFileName( node.InnerText ) );

                    //
                    // If we found a DLL in the project directory that does not exist in the RockWeb directory
                    // then copy it over.
                    //
                    if ( File.Exists( dllFile ) && !File.Exists( destFile ) )
                    {
                        File.Copy( dllFile, Path.Combine( destPath, Path.GetFileName( node.InnerText ) ) );
                    }
                }
            }
        }

        /// <summary>
        /// Customized version of the Support.ExtractZipFile method. We need to strip out any
        /// extra folders until we find the RockWeb folder and use it's parent as the root.
        /// TODO: This should be refactored to find the root folder and then use the standard
        /// extraction method by passing in the "stripFolder".
        /// </summary>
        /// <param name="archiveFilenameIn">The ZIP file on disk to be extracted.</param>
        /// <param name="outFolder">The destination folder to extract to.</param>
        private void ExtractZipFile( string archiveFilenameIn, string outFolder )
        {
            ZipFile zf = null;
            try
            {
                FileStream fs = File.OpenRead( archiveFilenameIn );
                zf = new ZipFile( fs );

                //
                // Find the parent directory of the RockWeb in the ZIP file and use that
                // as the root path.
                //
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

        #endregion

        #region Event Handlers

        /// <summary>
        /// Notification that the download has completed. Check for error and continue the operation.
        /// </summary>
        /// <param name="sender">The object that sent this event.</param>
        /// <param name="e">The arguments that describe this event.</param>
        private void WebClient_DownloadFileCompleted( object sender, System.ComponentModel.AsyncCompletedEventArgs e )
        {
            //
            // Check if there was an error downloading.
            //
            if ( e.Error != null )
            {
                UpdateStatusText( e.Error.Message );
                BuildCompleted?.Invoke( this, new EventArgs() );

                return;
            }

            //
            // Start the unpack process.
            //
            UnpackRelease( Path.Combine( Support.GetDataPath(), "temp.zip" ) );
        }

        /// <summary>
        /// Update the status text with the progress of the download.
        /// </summary>
        /// <param name="sender">The object that sent this event.</param>
        /// <param name="e">The arguments that describe this event.</param>
        private void WebClient_DownloadProgressChanged( object sender, DownloadProgressChangedEventArgs e )
        {
            //
            // Sadly, github doesn't tell us the final download size so we can't give
            // a percentage.
            //
            UpdateStatusText( string.Format( "Downloading {0:n0} MB...", e.BytesReceived / 1024.0d / 1024.0d ) );
        }

        /// <summary>
        /// Text has been received from one of the build tasks that needs to be displayed.
        /// </summary>
        /// <param name="sender">The object that sent this event.</param>
        /// <param name="text">The text received from the console application.</param>
        private void Console_StandardTextReceived( object sender, string text )
        {
            ConsoleOutput?.Invoke( this, text );
        }

        #endregion
    }
}
