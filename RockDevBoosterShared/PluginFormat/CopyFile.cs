namespace com.blueboxmoon.RockDevBooster.Shared.PluginFormat
{
    /// <summary>
    /// Defines the information in the plugin file for copying a single file.
    /// </summary>
    public class CopyFile
    {
        /// <summary>
        /// The source path, relative to the rockplugin.json file.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The destination path, relative to the RockWeb folder on the installation server.
        /// </summary>
        public string Destination { get; set; }
    }
}
