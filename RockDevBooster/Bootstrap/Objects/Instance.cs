using System;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using Jint;
using Newtonsoft.Json;

namespace com.blueboxmoon.RockDevBooster.Bootstrap.Objects
{
    /// <summary>
    /// Provides an interface to the Instances.
    /// </summary>
    public class Instance
    {
        /// <summary>
        /// Gets or sets the engine.
        /// </summary>
        /// <value>
        /// The engine.
        /// </value>
        public Engine Engine { get; set; }

        /// <summary>
        /// Gets or sets the instance name.
        /// </summary>
        /// <value>
        /// The instance name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Instance"/> class.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="name">The name.</param>
        public Instance( Engine engine, string name )
        {
            Engine = engine;
            Name = name;
        }

        /// <summary>
        /// Existses the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static bool Exists( string name )
        {
            string targetPath = Path.Combine( Support.GetInstancesPath(), name );

            return Directory.Exists( targetPath );
        }

        /// <summary>
        /// Starts the instance.
        /// </summary>
        public void Start()
        {
            Views.InstancesView.DefaultInstancesView.StartInstance( Name, 6229 );
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            Views.InstancesView.DefaultInstancesView.StopInstance();
        }

        /// <summary>
        /// Installs the plugin specified by the plugin spec file.
        /// </summary>
        /// <param name="pluginFile">The plugin spec file.</param>
        /// <exception cref="Exception"></exception>
        public void InstallPlugin( string pluginFile )
        {
            var rockWeb = Path.Combine( Path.Combine( Support.GetInstancesPath(), Name ), "RockWeb" );
            var pluginPath = Path.GetDirectoryName( pluginFile );

            if ( !Directory.Exists( rockWeb ) )
            {
                throw new Exception( string.Format( "Cannot install plugin '{0}'; instance '{1}' does not exist.", pluginFile, Name ) );
            }

            var pluginJson = File.ReadAllText( pluginFile );
            var plugin = JsonConvert.DeserializeObject<Shared.PluginFormat.Plugin>( pluginJson );
            plugin.ConfigureDefaults();

            var pluginBuilder = new Builders.PluginBuilder( plugin, pluginPath );

            var stream = pluginBuilder.Build();

            using ( var zf = new ZipFile( stream ) )
            {
                foreach ( ZipEntry entry in zf )
                {
                    if ( !entry.Name.StartsWith( "content/" ) )
                    {
                        continue;
                    }

                    if ( entry.IsFile )
                    {
                        string fullpath = Path.Combine( rockWeb, entry.Name.Replace( "content/", "" ) );
                        string directory = Path.GetDirectoryName( fullpath );

                        if ( !Directory.Exists( directory ) )
                        {
                            Directory.CreateDirectory( directory );
                        }

                        using ( FileStream streamWriter = File.Create( fullpath ) )
                        {
                            using ( var zipStream = zf.GetInputStream( entry ) )
                            {
                                zipStream.CopyTo( streamWriter );
                            }
                        }
                    }
                }

                var sqlInstallEntry = zf.GetEntry( "install/run.sql" );
                if ( sqlInstallEntry != null )
                {
                    string sqlScript;

                    using ( var zipStream = zf.GetInputStream( sqlInstallEntry ) )
                    {
                        using ( StreamReader reader = new StreamReader( zipStream, Encoding.UTF8 ) )
                        {
                            sqlScript = reader.ReadToEnd();
                        }
                    }

                    if ( !string.IsNullOrWhiteSpace( sqlScript ) )
                    {
                        using ( var connection = Views.InstancesView.DefaultInstancesView.GetSqlConnection() )
                        {
                            var command = connection.CreateCommand();
                            command.CommandText = sqlScript;
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Warmups this instance by loading the home page.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Warmup()
        {
            var request = System.Net.WebRequest.CreateHttp( "http://localhost:6229/" );
            var response = ( System.Net.HttpWebResponse ) request.GetResponse();

            if ( response.StatusCode != System.Net.HttpStatusCode.OK )
            {
                throw new Exception( string.Format( "Got invalid response code '{0}' during warmup.", response.StatusCode ) );
            }
        }
    }
}
