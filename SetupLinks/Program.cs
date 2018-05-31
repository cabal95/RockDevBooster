using System;
using System.IO;
using System.Runtime.InteropServices;

using Newtonsoft.Json;
using com.blueboxmoon.RockDevBooster.Shared.PluginFormat;

namespace SetupLinks
{
    class Program
    {
        [DllImport( "kernel32.dll" )]
        static extern bool CreateSymbolicLink( string lpSymlinkFileName, string lpTargetFileName, int dwFlags );

        static int Main( string[] args )
        {
            if ( args.Length < 2 )
            {
                return 1;
            }

            return SetupLinks( args[0], args[1] ) == true ? 0 : 1;
        }

        /// <summary>
        /// Creates a symbolic link between the two paths.
        /// </summary>
        /// <param name="path">The path where the symbolic link will be created.</param>
        /// <param name="target">The target that the symbolic link will point to.</param>
        /// <returns>True if the symbolic link was created.</returns>
        static private bool CreateSymbolicLink( string path, string target )
        {
            if ( Directory.Exists( target ) )
            {
                return CreateSymbolicLink( path, target, 1 );
            }
            else if ( File.Exists( target ) )
            {
                return CreateSymbolicLink( path, target, 0 );
            }

            return false;
        }

        /// <summary>
        /// Setups all the symbolic links for a plugin.
        /// </summary>
        /// <param name="pluginFile">The plugin file.</param>
        /// <param name="path">The path to the RockIt or RockWeb directory.</param>
        /// <returns></returns>
        static private bool SetupLinks( string pluginFile, string path )
        {
            var plugin = JsonConvert.DeserializeObject<Plugin>( File.ReadAllText( pluginFile ) );
            var pluginPath = Path.GetDirectoryName( pluginFile );
            string rockwebPath = null;
            string rockitPath = null;

            plugin.ConfigureDefaults();

            //
            // Check if this is a RockIt path or a Rock production path.
            //
            if ( Directory.Exists( Path.Combine( path, "RockWeb" ) ) )
            {
                rockwebPath = Path.Combine( path, "RockWeb" );
                rockitPath = path;
            }
            else
            {
                rockwebPath = path;
            }

            //
            // Setup all our path variables.
            //
            var pluginTld = plugin.Tld;
            var pluginOrganization = plugin.Organization.Replace( " ", "" ).ToLower();
            var pluginName = plugin.Name.Replace( " ", "" );

            var pluginControlsPath = Path.Combine( pluginPath, plugin.ControlsPath );
            var pluginThemesPath = Path.Combine( pluginPath, plugin.ThemesPath );
            var pluginWebhooksPath = Path.Combine( pluginPath, plugin.WebhooksPath );

            var rockwebPluginsPath = Path.Combine( rockwebPath, "Plugins" );
            var rockwebThemesPath = Path.Combine( rockwebPath, "Themes" );
            var rockwebWebhooksPath = Path.Combine( rockwebPath, "Webhooks" );
            var rockwebControlsPath = plugin.CombinePaths( rockwebPath, plugin.PluginPath );

            //
            // Do a final check to see if this does indeed appear to be a RockWeb folder.
            //
            if ( !Directory.Exists( rockwebPluginsPath ) || !Directory.Exists( rockwebWebhooksPath ) || !Directory.Exists( rockwebWebhooksPath ) )
            {
                Console.WriteLine( "Path provided does not appear to be a proper RockIt or Rock production path." );

                return false;
            }

            //
            // Setup the symlink for the rock blocks/controls.
            //
            if ( Directory.Exists( pluginControlsPath ) && !Directory.Exists( rockwebControlsPath ) )
            {
                if ( !Directory.Exists( Path.GetDirectoryName( rockwebControlsPath ) ) )
                {
                    Directory.CreateDirectory( Path.GetDirectoryName( rockwebControlsPath ) );
                }

                if ( !CreateSymbolicLink( rockwebControlsPath, pluginControlsPath ) )
                {
                    Console.WriteLine( "Failed to create symbolic link at '{0}'.", rockwebControlsPath );

                    return false;
                }

                Console.WriteLine( "Created symbolic link at '{0}'.", rockwebControlsPath );
            }

            //
            // Setup the symlinks for any themes.
            //
            if ( Directory.Exists( pluginThemesPath ) )
            {
                foreach ( var d in Directory.EnumerateDirectories( pluginThemesPath ) )
                {
                    var destPath = Path.Combine( rockwebThemesPath, Path.GetFileName( d ) );

                    if ( !Directory.Exists( destPath ) )
                    {
                        if ( !CreateSymbolicLink( destPath, d ) )
                        {
                            Console.WriteLine( "Failed to create symbolic link at '{0}'", destPath );

                            return false;
                        }

                        Console.WriteLine( "Created symbolic link at '{0}'.", destPath );
                    }
                }
            }

            //
            // Setup the symlinks for any webhooks.
            //
            if ( Directory.Exists( pluginWebhooksPath ) )
            {
                foreach ( var f in Directory.EnumerateFiles( pluginWebhooksPath ) )
                {
                    var destPath = Path.Combine( rockwebWebhooksPath, Path.GetFileName( f ) );

                    if ( !File.Exists( destPath ) )
                    {
                        if ( !CreateSymbolicLink( destPath, f ) )
                        {
                            Console.WriteLine( "Failed to create symbolic link at '{0}'", destPath );

                            return false;
                        }

                        Console.WriteLine( "Created symbolic link at '{0}'.", destPath );
                    }
                }
            }

            //
            // Setup the symlink for the main project.
            //
            if ( rockitPath != null )
            {
                string destPath = string.Format( "{0}.{1}.{2}",
                    plugin.Tld.ToLower(),
                    plugin.Organization.ToLower().Replace( " ", "" ),
                    plugin.Name.Replace( " ", "" ) );

                destPath = Path.Combine( rockitPath, destPath );

                if ( !Directory.Exists( destPath ) )
                {
                    if ( !CreateSymbolicLink( destPath, pluginPath ) )
                    {
                        Console.WriteLine( "Failed to create symbolic link at '{0}'", destPath );

                        return false;
                    }

                    Console.WriteLine( "Created symbolic link at '{0}'.", destPath );
                }
            }

            return true;
        }
    }
}
