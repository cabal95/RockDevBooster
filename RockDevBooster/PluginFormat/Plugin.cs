using System.Collections.Generic;

namespace com.blueboxmoon.RockDevBooster.PluginFormat
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
            if ( string.IsNullOrWhiteSpace( ProjectFile ) )
            {
                ProjectFile = string.Format( "com.{0}.{1}.csproj", Organization.ToLower().Replace( " ", "" ), Name.Replace( " ", "" ) );
            }

            if ( string.IsNullOrWhiteSpace( PluginPath ) )
            {
                PluginPath = string.Format( "Plugins/com_{0}/{1}", Organization.ToLower().Replace( " ", "" ), Name.Replace( " ", "" ) );
            }

            ControlsPath = ControlsPath ?? "Controls";
            ThemesPath = ThemesPath ?? "Themes";
            WebhooksPath = WebhooksPath ?? "Webhooks";
            DLLs = DLLs ?? new List<string>();
            Copy = Copy ?? new List<CopyFile>();
            RemoveFilesOnInstall = RemoveFilesOnInstall ?? new List<string>();
            RemoveFilesOnUninstall = RemoveFilesOnUninstall ?? new List<string>();
        }
    }
}
