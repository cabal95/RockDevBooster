using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using com.blueboxmoon.RockDevBooster.Shared.PluginFormat;

namespace com.blueboxmoon.RockDevBooster.Builders
{
    public class PluginBuilder
    {
        #region Public Properties

        /// <summary>
        /// Notification that a message is to be logged.
        /// </summary>
        public event EventHandler<string> LogMessage;

        /// <summary>
        /// Gets or sets the plugin path.
        /// </summary>
        /// <value>
        /// The plugin path.
        /// </value>
        public string PluginPath { get; set; }

        /// <summary>
        /// Gets or sets the plugin.
        /// </summary>
        /// <value>
        /// The plugin.
        /// </value>
        public Plugin Plugin { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginBuilder"/> class.
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        /// <param name="pluginPath">The plugin path.</param>
        public PluginBuilder( Plugin plugin, string pluginPath )
        {
            Plugin = plugin;
            PluginPath = pluginPath;
        }

        #endregion

        /// <summary>
        /// Combine two path components into a final path.
        /// </summary>
        /// <param name="mainPath">The left side of the path to be combined.</param>
        /// <param name="path">The right side of the path to be combined.</param>
        /// <returns>The full combined path.</returns>
        protected string CombinePaths( string mainPath, string path )
        {
            List<string> paths = new List<string> { mainPath };

            paths.AddRange( path.Split( '/', '\\' ) );

            return Path.Combine( paths.ToArray() );
        }

        /// <summary>
        /// Copy all files and directories recursively from the source path to the destination path.
        /// </summary>
        /// <param name="sourcePath">The path whose contents will be copied.</param>
        /// <param name="destinationPath">The path where the contents will be copied into.</param>
        protected void CopyFiles( string sourcePath, string destinationPath )
        {
            if ( Directory.Exists( sourcePath ) )
            {
                var files = GetFileList( sourcePath );
                string stripName = sourcePath.EndsWith( "\\" ) ? sourcePath : string.Format( "{0}\\", sourcePath );

                foreach ( var file in files )
                {
                    var strippedFile = file.Replace( stripName, string.Empty );
                    var dest = CombinePaths( destinationPath, strippedFile );

                    CopyFile( file, dest );
                }
            }
        }

        /// <summary>
        /// Copy a file and log the message.
        /// </summary>
        /// <param name="sourcePath">The source file to copy.</param>
        /// <param name="destinationPath">The destination file path to copy to.</param>
        protected void CopyFile( string sourcePath, string destinationPath )
        {
            LogMessage?.Invoke( this, string.Format( "Copying \"{0}\" to \"{1}\"\n", sourcePath, destinationPath ) );

            if ( !Directory.Exists( Path.GetDirectoryName( destinationPath ) ) )
            {
                Directory.CreateDirectory( Path.GetDirectoryName( destinationPath ) );
            }

            File.Copy( sourcePath, destinationPath );
        }

        /// <summary>
        /// Copies all the DLLs from the bin\Release directory of the project into
        /// the destination path.
        /// </summary>
        /// <param name="dlls">A list of DLL names to be copied.</param>
        /// <param name="destinationPath">The path to copy all the files to.</param>
        protected void CopyDLLs( string projectPath, List<string> dlls, string destinationPath )
        {
            foreach ( var file in dlls )
            {
                string dest = Path.Combine( destinationPath, file );

                if ( !Directory.Exists( Path.GetDirectoryName( dest ) ) )
                {
                    Directory.CreateDirectory( Path.GetDirectoryName( dest ) );
                }

                CopyFile( Path.Combine( projectPath, "bin", "Release", file ), Path.Combine( destinationPath, file ) );
            }
        }

        /// <summary>
        /// Gets a list of absolute paths to all files contained in the directory, recursively.
        /// </summary>
        /// <param name="path">The path to be searched.</param>
        /// <returns>A list of strings representing the file names and paths.</returns>
        protected List<string> GetFileList( string path )
        {
            var files = new List<string>();

            foreach ( string f in Directory.GetFiles( path ) )
            {
                files.Add( f );
            }

            foreach ( string d in Directory.GetDirectories( path ) )
            {
                files.AddRange( GetFileList( d ) );
            }

            return files;
        }

        /// <summary>
        /// Build a package from all the components available.
        /// </summary>
        public Stream Build()
        {
            //
            // Get all our staging paths.
            //
            var stagingPath = Support.GetPackageBuildPath();
            var contentPath = Path.Combine( stagingPath, "content" );
            var installPath = Path.Combine( stagingPath, "install" );
            var uninstallPath = Path.Combine( stagingPath, "uninstall" );


            //
            // Delete any failed builds.
            //
            if ( Directory.Exists( stagingPath ) )
            {
                Directory.Delete( stagingPath, true );
            }

            //
            // Create all our staging paths.
            //
            Directory.CreateDirectory( stagingPath );
            Directory.CreateDirectory( contentPath );
            Directory.CreateDirectory( installPath );
            Directory.CreateDirectory( uninstallPath );

            //
            // Copy the Controls, Themes, Webhooks.
            //
            CopyFiles( Plugin.CombinePaths( PluginPath, Plugin.ControlsPath ), Plugin.CombinePaths( contentPath, Plugin.PluginPath ) );
            CopyFiles( Plugin.CombinePaths( PluginPath, Plugin.ThemesPath ), CombinePaths( contentPath, "Themes" ) );
            CopyFiles( Plugin.CombinePaths( PluginPath, Plugin.WebhooksPath ), CombinePaths( contentPath, "Webhooks" ) );

            //
            // Copy DLLs.
            //
            string projectFile = Path.GetFullPath( Plugin.CombinePaths( PluginPath, Plugin.ProjectFile ) );
            if ( File.Exists( projectFile ) )
            {
                var dlls = GetFileList( Path.Combine( PluginPath, "obj", "Release" ) )
                    .Select( f => Path.GetFileName( f ) )
                    .Where( f => f.EndsWith( ".dll" ) )
                    .ToList();
                dlls.AddRange( Plugin.DLLs );
                CopyDLLs( Path.GetDirectoryName( Plugin.CombinePaths( PluginPath, Plugin.ProjectFile ) ), dlls, Path.Combine( contentPath, "bin" ) );
            }

            //
            // Copy the SQL scripts.
            //
            if ( !string.IsNullOrWhiteSpace( Plugin.InstallSql ) )
            {
                CopyFile( Plugin.CombinePaths( PluginPath, Plugin.InstallSql ), Path.Combine( installPath, "run.sql" ) );
            }
            if ( !string.IsNullOrWhiteSpace( Plugin.UninstallSql ) )
            {
                CopyFile( Plugin.CombinePaths( PluginPath, Plugin.UninstallSql ), Path.Combine( uninstallPath, "run.sql" ) );
            }

            //
            // Copy any additional files.
            //
            foreach ( var file in Plugin.Copy )
            {
                CopyFile( Plugin.CombinePaths( PluginPath, file.Source ), Plugin.CombinePaths( contentPath, file.Destination ) );
            }

            //
            // Build the install deletefile.lst.
            //
            var files = Plugin.RemoveFilesOnInstall.Select( f => f.Replace( "/", "\\" ) ).ToList();
            File.WriteAllLines( Path.Combine( installPath, "deletefile.lst" ), files.ToArray() );

            //
            // Build the uninstall deletefile.lst.
            //
            string stripPath = string.Format( "{0}\\", contentPath );
            files = GetFileList( contentPath ).Select( f => f.Replace( stripPath, string.Empty ) ).ToList();
            files.AddRange( Plugin.RemoveFilesOnUninstall.Select( f => f.Replace( "/", "\\" ) ) );
            File.WriteAllLines( Path.Combine( uninstallPath, "deletefile.lst" ), files.ToArray() );

            //
            // Zip it all up.
            //
            LogMessage?.Invoke( this, "Compressing zip file\n" );

            return Support.CreateZipStreamFromFolder( stagingPath );
        }
    }
}
