using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace com.blueboxmoon.RockDevBooster.Builders
{
    public class ProjectBuilder
    {
        #region Internal Fields

        /// <summary>
        /// The status text that we last knew. We track this so we don't send duplicate
        /// status updates.
        /// </summary>
        protected string StatusText { get; private set; }

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
        /// Execute NuGet to restore all package references.
        /// </summary>
        /// <returns>True if the operation succeeded.</returns>
        private bool NuGetRestore( string projectPath )
        {
            //
            // Check if the project even uses NuGet packages.
            //
            if ( !File.Exists( Path.Combine( projectPath, "packages.config" ) ) )
            {
                return true;
            }

            //
            // Execute 'nuget.exe restore' in the solution directory.
            //
            var process = new Utilities.ConsoleApp( Path.Combine( Environment.CurrentDirectory, "nuget.exe" ) )
            {
                WorkingDirectory = projectPath
            };
            process.StandardTextReceived += Console_StandardTextReceived;
            process.ErrorTextReceived += Console_StandardTextReceived;
            process.ExecuteAsync( "restore", "-PackagesDirectory", "packages" );

            //
            // Wait for it to finish.
            //
            while ( process.Running )
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

        #endregion

        /// <summary>
        /// Build the project so we have compiled DLLs.
        /// </summary>
        public bool BuildProject( string projectFile, string msbuild )
        {
            //
            // Restore any NuGet packages.
            //
            UpdateStatusText( "Restoring References" );
            if ( !NuGetRestore( Path.GetDirectoryName( projectFile ) ) )
            {
                return false;
            }

            //
            // Launch a new devenv.com process to build the solution.
            //
            UpdateStatusText( "Building..." );
            var process = new Utilities.ConsoleApp( msbuild );
            process.StandardTextReceived += Console_StandardTextReceived;
            process.ErrorTextReceived += Console_StandardTextReceived;
            process.WorkingDirectory = Path.GetDirectoryName( projectFile );
            process.ExecuteAsync( projectFile, "/P:Configuration=Release" );

            //
            // Wait for it to complete.
            //
            while ( process.Running )
            {
                Task.Delay( 100 ).Wait();
            }

            //
            // Check if our build worked or not.
            //
            if ( process.ExitCode != 0 )
            {
                return false;
            }

            return true;
        }

        #region Event Handlers

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
