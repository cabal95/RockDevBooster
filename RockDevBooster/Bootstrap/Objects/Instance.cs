using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        #region Properties

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
        public string Name { get; private set; }

        #endregion

        #region Constructors

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

        #endregion

        #region Static Methods

        /// <summary>
        /// Retrieves the names of all instances in the system.
        /// </summary>
        /// <returns>An array of instances names.</returns>
        public static string[] All()
        {
            return Directory.GetDirectories( Support.GetInstancesPath() )
                .Select( d => Path.GetFileName( d ) )
                .Select( f => f.Substring( 0, f.Length ) )
                .ToArray();
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
        /// Prompts the user to select an instance.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <returns>An instance or null if the user cancelled.</returns>
        public static Instance Prompt( Engine engine )
        {
            return Prompt( engine, "Select Instance" );
        }

        /// <summary>
        /// Prompts the user to select an instance.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="title">The title of the dialog.</param>
        /// <returns>An instance or null if the user cancelled.</returns>
        public static Instance Prompt( Engine engine, string title )
        {
            string instanceName = null;
            var instances = All().ToList();

            System.Windows.Application.Current.Dispatcher.Invoke( () =>
            {
                var dialog = new Dialogs.ComboBoxInputDialog( null, title );

                dialog.Items = instances;
                dialog.Required = true;

                if ( dialog.ShowDialog() == false )
                {
                    instanceName = null;
                }
                else
                {
                    instanceName = dialog.SelectedValue;
                }
            } );

            return new Instance( engine, instanceName );
        }

        /// <summary>
        /// Deletes the specified instance.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <exception cref="Exception"></exception>
        public static void Delete( string name )
        {
            if ( name == Views.InstancesView.RunningInstanceName )
            {
                throw new Exception( string.Format( "The instance '{0}' cannot be deleted because it is running.", name ) );
            }

            string path = Path.Combine( Support.GetInstancesPath(), name );

            Directory.Delete( path, true );

            Views.InstancesView.UpdateInstances();
        }

        #endregion

        #region Public Methods

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
            InstallPlugin( pluginFile, false );
        }

        /// <summary>
        /// Installs the plugin specified by the plugin spec file.
        /// </summary>
        /// <param name="pluginFile">The plugin spec file.</param>
        /// <exception cref="Exception"></exception>
        public void InstallPlugin( string pluginFile, bool verbose )
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

            var logger = new EventHandler<string>( ( sender, message ) =>
            {
                if ( verbose )
                {
                    if ( message.EndsWith( "\n" ) )
                    {
                        message = message.Substring( 0, message.Length - 1 );
                    }
                    if ( message.EndsWith( "\r" ) )
                    {
                        message = message.Substring( 0, message.Length - 1 );
                    }

                    var bootstrap = ( Bootstrapper ) ( ( Jint.Runtime.Interop.ObjectWrapper ) Engine.GetValue( "__Bootstrap" ).AsObject() ).Target;
                    bootstrap.Log( message );
                }
            } );

            //
            // Initialize a new release builder to process this import operation.
            //
            var projectBuilder = new Builders.ProjectBuilder();
            projectBuilder.ConsoleOutput += logger;

            if ( !projectBuilder.BuildProject( plugin.CombinePaths( pluginPath, plugin.ProjectFile ), VisualStudioInstall.GetDefaultInstall().GetMsBuild() ) )
            {
                throw new Exception( "Install Plugin Failed" );
            }

            var pluginBuilder = new Builders.PluginBuilder( plugin, pluginPath );
            pluginBuilder.LogMessage += logger;

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

        /// <summary>
        /// Executes the SQL statement.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        public void ExecuteSqlStatement( string sql )
        {
            using ( var connection = Views.InstancesView.DefaultInstancesView.GetSqlConnection() )
            {
                var command = connection.CreateCommand();
                command.CommandText = sql;

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes the scalar SQL statement.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        public object ExecuteSqlScalar( string sql )
        {
            using ( var connection = Views.InstancesView.DefaultInstancesView.GetSqlConnection() )
            {
                var command = connection.CreateCommand();
                command.CommandText = sql;

                return command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Executes the SQL statement.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        public Dictionary<string, object>[] ExecuteSql( string sql )
        {
            using ( var connection = Views.InstancesView.DefaultInstancesView.GetSqlConnection() )
            {
                var command = connection.CreateCommand();
                command.CommandText = sql;

                using ( var reader = command.ExecuteReader() )
                {
                    var results = new List<Dictionary<string, object>>();

                    while ( reader.Read() )
                    {
                        var row = new Dictionary<string, object>();

                        for ( int i = 0; i < reader.FieldCount; i++ )
                        {
                            row.Add( reader.GetName( i ), reader[i] );
                        }

                        results.Add( row );
                    }

                    return results.ToArray();
                }
            }
        }

        #endregion
    }
}
