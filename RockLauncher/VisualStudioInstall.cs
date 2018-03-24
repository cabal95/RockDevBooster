using System.IO;

namespace com.blueboxmoon.RockLauncher
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
    }
}
