using System.Collections.Generic;
using System.IO;

namespace com.blueboxmoon.RockDevBooster.Shared.PluginFormat
{
    public class Plugin
    {
        /// <summary>
        /// [Required] The name of the plugin.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// [Required] The name of the organization releasing this plugin.
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        /// Gets or sets the TLD for path operations. Defaults to "com".
        /// </summary>
        public string Tld { get; set; }

        /// <summary>
        /// The path and filename to the csproj file to be built along with this plugin.
        /// </summary>
        public string ProjectFile { get; set; }

        /// <summary>
        /// The path to the plugin folder on the server, e.g. Plugins/com_rocksolidchurchdemo/MyPlugin
        /// </summary>
        public string PluginPath { get; set; }

        /// <summary>
        /// The path to the source user controls in the project, relative to the rockplugin.json file.
        /// </summary>
        public string ControlsPath { get; set; }

        /// <summary>
        /// The path to the source themes in the project, relative to the rockplugin.json file.
        /// </summary>
        public string ThemesPath { get; set; }

        /// <summary>
        /// The path to the source webhooks in the project, relative to the rockplugin.json file.
        /// </summary>
        public string WebhooksPath { get; set; }

        /// <summary>
        /// The path to the install SQL script file, relative to the rockplugin.json file.
        /// </summary>
        public string InstallSql { get; set; }

        /// <summary>
        /// The path to the uninstall SQL script file, relative to the rockplugin.json file.
        /// </summary>
        public string UninstallSql { get; set; }

        /// <summary>
        /// The DLL filenames to be copied from the projects bin/Release folder. The DLL built
        /// from the project file itself will already have been copied.
        /// </summary>
        public List<string> DLLs { get; set; }

        /// <summary>
        /// A list of files to be copied that could not be found by any of the standard methods.
        /// </summary>
        public List<CopyFile> Copy { get; set; }

        /// <summary>
        /// A list of files (relative paths to the RockWeb folder) to be removed during installation.
        /// </summary>
        public List<string> RemoveFilesOnInstall { get; set; }

        /// <summary>
        /// A list of files (relative paths to the RockWeb folder) to be removed during uninstall.
        /// </summary>
        public List<string> RemoveFilesOnUninstall { get; set; }

        /// <summary>
        /// Prepares the object by defining any default values for properties that have not
        /// yet been set.
        /// </summary>
        public void ConfigureDefaults()
        {
            Tld = Tld ?? "com";

            if ( string.IsNullOrWhiteSpace( ProjectFile ) )
            {
                ProjectFile = string.Format( "{2}.{0}.{1}.csproj", Organization.ToLower().Replace( " ", "" ), Name.Replace( " ", "" ), Tld );
            }

            if ( string.IsNullOrWhiteSpace( PluginPath ) )
            {
                PluginPath = string.Format( "Plugins/{2}_{0}/{1}", Organization.ToLower().Replace( " ", "" ), Name.Replace( " ", "" ), Tld );
            }

            ControlsPath = ControlsPath ?? "Controls";
            ThemesPath = ThemesPath ?? "Themes";
            WebhooksPath = WebhooksPath ?? "Webhooks";
            DLLs = DLLs ?? new List<string>();
            Copy = Copy ?? new List<CopyFile>();
            RemoveFilesOnInstall = RemoveFilesOnInstall ?? new List<string>();
            RemoveFilesOnUninstall = RemoveFilesOnUninstall ?? new List<string>();
        }

        /// <summary>
        /// Translate a path by replacing special character strings with references
        /// in the plugin data.
        /// </summary>
        /// <param name="path">The path string to be translated.</param>
        /// <returns>A new path with the replacements done.</returns>
        public string TranslatePluginPath( string path )
        {
            return path
                .Replace( "{Name}", Name )
                .Replace( "{Organization}", Organization )
                .Replace( "{PluginPath}", PluginPath )
                .Replace( "{ControlsPath}", ControlsPath )
                .Replace( "{ThemesPath}", ThemesPath )
                .Replace( "{WebhooksPath}", WebhooksPath );
        }

        /// <summary>
        /// Combine two path components into a final path.
        /// </summary>
        /// <param name="mainPath">The left side of the path to be combined.</param>
        /// <param name="path">The right side of the path to be combined.</param>
        /// <returns>The full combined path.</returns>
        public string CombinePaths( string mainPath, string path )
        {
            List<string> paths = new List<string> { mainPath };

            path = TranslatePluginPath( path );

            paths.AddRange( path.Split( '/', '\\' ) );

            return Path.Combine( paths.ToArray() );
        }
    }
}
