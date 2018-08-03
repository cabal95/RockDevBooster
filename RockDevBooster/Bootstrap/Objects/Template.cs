using System;
using System.IO;
using System.Linq;

using Jint;

namespace com.blueboxmoon.RockDevBooster.Bootstrap.Objects
{
    /// <summary>
    /// Provides a JS interface to the Templates.
    /// </summary>
    public class Template
    {
        /// <summary>
        /// Gets or sets the engine.
        /// </summary>
        /// <value>
        /// The engine.
        /// </value>
        private Engine Engine { get; set; }

        /// <summary>
        /// Gets or sets the name of the template.
        /// </summary>
        /// <value>
        /// The name of the template.
        /// </value>
        private string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Template"/> class.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="name">The name.</param>
        public Template( Engine engine, string name )
        {
            Engine = engine;
            Name = name;
        }

        /// <summary>
        /// Retrieves the names of all templates in the system.
        /// </summary>
        /// <returns>An array of template names.</returns>
        public static string[] All()
        {
            return Directory.GetFiles( Support.GetTemplatesPath(), "*.zip" )
                .Select( d => Path.GetFileName( d ) )
                .Select( f => f.Substring( 0, f.Length - 4 ) )
                .ToArray();
        }

        /// <summary>
        /// Checks if the named template exists.
        /// </summary>
        /// <param name="name">The name of the template.</param>
        /// <returns>true if the template exists.</returns>
        public static bool Exists( string name )
        {
            string zipfile = Path.Combine( Support.GetTemplatesPath(), name + ".zip" );

            return File.Exists( zipfile );
        }

        /// <summary>
        /// Deploys the template to the named instance.
        /// </summary>
        /// <param name="instanceName">The instance name.</param>
        /// <returns>A new Instance object.</returns>
        public Instance Deploy( string instanceName, Action<string, double> progressCallback )
        {
            string zipfile = Path.Combine( Support.GetTemplatesPath(), Name + ".zip" );
            string targetPath = Path.Combine( Support.GetInstancesPath(), instanceName );

            if ( Directory.Exists( targetPath ) )
            {
                throw new Exception( string.Format( "Instance '{0}' already exists", instanceName ) );
            }

            //
            // Extract the zip file to the target instance path.
            //
            Support.ExtractZipFile( zipfile, Path.Combine( targetPath, "RockWeb" ), ( progress ) =>
            {
                progressCallback?.Invoke( string.Format( "Extracting {0:n0}%...", Math.Floor( progress * 100 ) ), progress );
            } );

            //
            // Update the UI to indicate that it is deployed.
            //
            Views.InstancesView.UpdateInstances();

            return new Instance( Engine, instanceName );
        }
    }
}
