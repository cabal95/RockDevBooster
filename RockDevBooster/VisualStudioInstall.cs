using System.Collections.Generic;
using System.Linq;
using System.IO;

using Microsoft.VisualStudio.Setup.Configuration;
using com.blueboxmoon.RockDevBooster.Properties;

namespace com.blueboxmoon.RockDevBooster
{
    /// <summary>
    /// Identifies a single Visual Studio installation on disk.
    /// </summary>
    public class VisualStudioInstall
    {
        /// <summary>
        /// The user friendly name of the installation.
        /// </summary>
        public string Name { get; set; }

        //
        // The path to the installation.
        //
        public string Path { get; set; }

        /// <summary>
        /// Get the path to the devenv.com executable for this installation.
        /// </summary>
        /// <returns>A full filesystem path or null if not found.</returns>
        public string GetExecutable()
        {
            return FindExecutable( Path, "devenv.com" );
        }

        /// <summary>
        /// Get the path to the msbuild.exe executable for this installation.
        /// </summary>
        /// <returns>A full filesystem path or null if not found.</returns>
        public string GetMsBuild()
        {
            return FindExecutable( Path, "msbuild.exe" );
        }

        /// <summary>
        /// Searches the installation tree recursively for the file.
        /// </summary>
        /// <param name="path">The path to search for the filename.</param>
        /// <param name="filename">The filename to be searched for.</param>
        /// <returns>A path to the filename or null if not found.</returns>
        private string FindExecutable( string path, string filename )
        {
            string fullPath = System.IO.Path.Combine( path, filename );

            if ( File.Exists( fullPath ) )
            {
                return fullPath;
            }

            var dirs = Directory.GetDirectories( path );
            foreach ( var dir in dirs )
            {
                fullPath = FindExecutable( dir, filename );
                if ( fullPath != null )
                {
                    return fullPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Generate a user friendly string that identifies this object.
        /// </summary>
        /// <returns>A string which identifies this object.</returns>
        public override string ToString()
        {
            return Name;
        }

        #region Static Methods

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

        /// <summary>
        /// Gets the current install selected by the user, or the first installation.
        /// </summary>
        /// <returns>The visual studio install to use when building.</returns>
        static public VisualStudioInstall GetDefaultInstall()
        {
            var instances = GetVisualStudioInstances();
            var currentPath = Settings.Default.VisualStudioVersion;

            var current = instances.Where( i => i.Path == currentPath ).FirstOrDefault();

            if ( current == null && instances.Count > 0 )
            {
                current = instances.First();
            }

            return current;
        }

        #endregion
    }
}
