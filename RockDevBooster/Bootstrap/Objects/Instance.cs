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

        /// <summary>
        /// Stops all currently running instances.
        /// </summary>
        public static void StopAll()
        {
            Views.InstancesView.DefaultInstancesView.StopInstance();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets the results from command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        protected List<Dictionary<string, object>> GetResultsFromCommand( System.Data.SqlClient.SqlCommand command )
        {
            try
            {
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

                    return results;
                }
            }
            catch ( Exception e )
            {
                System.Diagnostics.Debugger.Launch();
                System.Diagnostics.Debugger.Break();
                throw e;
            }
        }

        /// <summary>
        /// Resolves the instance path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        protected string ResolveInstancePath( string path )
        {
            if ( !path.StartsWith( "~" ) )
            {
                return path;
            }

            if ( path.StartsWith( @"~\" ) )
            {
                path = path.Substring( 2 );
            }
            else
            {
                path = path.Substring( 1 );
            }

            return Path.Combine( Support.GetInstancesPath(), Name, "RockWeb", path );
        }

        /// <summary>
        /// Simplifies the instance path by replacing the instance RockWeb path with ~.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        protected string SimplifyInstancePath( string path )
        {
            string instancePath = Path.Combine( Support.GetInstancesPath(), Name, "RockWeb" );

            return path.Replace( instancePath, "~" );
        }

        /// <summary>
        /// Directories the copy.
        /// </summary>
        /// <param name="sourceDirName">Name of the source dir.</param>
        /// <param name="destDirName">Name of the dest dir.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        protected void DirectoryCopy( string sourceDirName, string destDirName, Action<string, string> logger = null )
        {
            //
            // Get the subdirectories for the specified directory.
            //
            DirectoryInfo dir = new DirectoryInfo( sourceDirName );

            if ( !dir.Exists )
            {
                throw new DirectoryNotFoundException( $"Source directory '{ sourceDirName }' does not exist or could not be found." );
            }

            //
            // If the destination directory doesn't exist, create it.
            //
            if ( !Directory.Exists( destDirName ) )
            {
                Directory.CreateDirectory( destDirName );
            }

            //
            // Get the files in the directory and copy them to the new location.
            //
            FileInfo[] files = dir.GetFiles();
            foreach ( FileInfo file in files )
            {
                string temppath = Path.Combine( destDirName, file.Name );
                file.CopyTo( temppath, false );
                logger( file.FullName, temppath );
            }

            //
            // Get the sub directories and copy them to the new location.
            //
            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach ( DirectoryInfo subdir in dirs )
            {
                DirectoryCopy( subdir.FullName, Path.Combine( destDirName, subdir.Name ), logger );
            }
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
                throw new Exception( "Build Plugin Failed" );
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
        /// Copies a file into the RockWeb folder.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        public void CopyFile( string source, string destination )
        {
            CopyFile( source, destination, false );
        }

        /// <summary>
        /// Copies a file into the RockWeb folder.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="verbose">if set to <c>true</c> [verbose].</param>
        public void CopyFile( string source, string destination, bool verbose )
        {
            source = source.Replace( '/', '\\' );
            destination = destination.Replace( '/', '\\' );

            if ( destination.Contains( ".." ) )
            {
                throw new ArgumentException( "Destination cannot include '..' in the path.", "destination" );
            }

            if ( !destination.StartsWith( @"~\" ) )
            {
                throw new ArgumentException( "Destination must begin with '~\\'.", "destination" );
            }

            source = ResolveInstancePath( source );
            destination = ResolveInstancePath( destination );

            if ( File.Exists( destination ) || Directory.Exists( destination ) )
            {
                throw new Exception( $"Destination '{destination}' already exists." );
            }

            Action<string, string> logger = delegate ( string src, string dest )
            {
                if ( verbose )
                {
                    var bootstrap = ( Bootstrapper ) ( ( Jint.Runtime.Interop.ObjectWrapper ) Engine.GetValue( "__Bootstrap" ).AsObject() ).Target;
                    bootstrap.Log( $"Copied '{SimplifyInstancePath( src )}' to '{SimplifyInstancePath( dest )}'." );
                }
            };

            if ( File.Exists( source ) )
            {
                File.Copy( source, destination );
                logger( source, destination );
            }
            else
            {
                DirectoryCopy( source, destination, logger );
            }
        }

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="path">The path.</param>
        public void DeleteFile( string path )
        {
            DeleteFile( path, false );
        }

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="verbose">if set to <c>true</c> [verbose].</param>
        public void DeleteFile( string path, bool verbose )
        {
            path = path.Replace( '/', '\\' );

            if ( path.Contains( ".." ) )
            {
                throw new ArgumentException( "Destination cannot include '..' in the path.", "path" );
            }

            if ( !path.StartsWith( @"~\" ) )
            {
                throw new ArgumentException( "Destination must begin with '~\\'.", "path" );
            }

            var fullPath = ResolveInstancePath( path );

            if ( File.Exists( fullPath ) )
            {
                File.Delete( fullPath );

                if ( verbose )
                {
                    var bootstrap = ( Bootstrapper ) ( ( Jint.Runtime.Interop.ObjectWrapper ) Engine.GetValue( "__Bootstrap" ).AsObject() ).Target;
                    bootstrap.Log( $"Deleted file '{ SimplifyInstancePath( fullPath ) }'." );
                }
            }
            else if ( Directory.Exists( fullPath ) )
            {
                Directory.Delete( fullPath, true );

                if ( verbose )
                {
                    var bootstrap = ( Bootstrapper ) ( ( Jint.Runtime.Interop.ObjectWrapper ) Engine.GetValue( "__Bootstrap" ).AsObject() ).Target;
                    bootstrap.Log( $"Deleted directory '{ SimplifyInstancePath( fullPath ) }'." );
                }
            }
            else
            {
                throw new FileNotFoundException( $"Path '{ path }' does not exist." );
            }
        }

        /// <summary>
        /// Checks if the file the exists.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public bool FileExists( string path )
        {
            path = path.Replace( '/', '\\' );

            return File.Exists( ResolveInstancePath( path ) );
        }

        /// <summary>
        /// Checks if the directory the exists.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public bool DirectoryExists( string path )
        {
            path = path.Replace( '/', '\\' );

            return File.Exists( ResolveInstancePath( path ) );
        }

        /// <summary>
        /// Warmups this instance by loading the home page.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Warmup()
        {
            var request = System.Net.WebRequest.CreateHttp( "http://localhost:6229/" );

            request.Timeout = 600000; /* 10 minutes */

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = ( System.Net.HttpWebResponse ) request.GetResponse();
            sw.Stop();

            var bootstrap = ( Bootstrapper ) ( ( Jint.Runtime.Interop.ObjectWrapper ) Engine.GetValue( "__Bootstrap" ).AsObject() ).Target;
            bootstrap.Log( $"Warmup took ${ sw.ElapsedMilliseconds } ms" );

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

                return GetResultsFromCommand( command ).ToArray();
            }
        }

        /// <summary>
        /// Determines whether any migration exceptions have occurred since the script started.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if any migration exceptions have occurred; otherwise, <c>false</c>.
        /// </returns>
        public bool HasRecentMigrationExceptions()
        {
            return HasRecentMigrationExceptions( false );
        }

        /// <summary>
        /// Determines whether any migration exceptions have occurred since the script started.
        /// </summary>
        /// <param name="logExceptions">if set to <c>true</c> the exceptions should be logged.</param>
        /// <returns>
        ///   <c>true</c> if any migration exceptions have occurred; otherwise, <c>false</c>.
        /// </returns>
        public bool HasRecentMigrationExceptions( bool logExceptions )
        {
            var bootstrap = ( Bootstrapper ) ( ( Jint.Runtime.Interop.ObjectWrapper ) Engine.GetValue( "__Bootstrap" ).AsObject() ).Target;
            //            bootstrap.Log( message );

            //
            // Get any recent exceptions.
            //
            using ( var connection = Views.InstancesView.DefaultInstancesView.GetSqlConnection() )
            {
                var command = connection.CreateCommand();
                command.CommandText = @"SELECT [Id],[HasInnerException],[Description]
FROM [ExceptionLog]
WHERE [CreatedDateTime] >= @LimitDate
  AND [Description] LIKE 'Plugin Migration error%'";
                command.Parameters.AddWithValue( "LimitDate", bootstrap.ExecuteStartedDateTime );

                var baseExceptions = GetResultsFromCommand( command );

                //
                // If requested, log the exceptions.
                //
                if ( logExceptions )
                {
                    foreach ( var exception in baseExceptions )
                    {
                        var data = exception;

                        while ( data != null )
                        {
                            bootstrap.Log( data["Description"] );

                            if ( ( bool ) data["HasInnerException"] )
                            {
                                using ( var cmd = connection.CreateCommand() )
                                {
                                    cmd.CommandText = @"SELECT TOP 1 [Id],[HasInnerException],[Description] FROM [ExceptionLog] WHERE [ParentId] = @Id";
                                    cmd.Parameters.AddWithValue( "Id", data["Id"] );

                                    var dataItems = GetResultsFromCommand( cmd );
                                    data = dataItems.Any() ? dataItems[0] : null;
                                }
                            }
                            else
                            {
                                data = null;
                            }
                        }
                    }
                }

                return baseExceptions.Any();
            }
        }

        #endregion
    }
}
