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
        #region Properties

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
        public string Name { get; private set; }

        #endregion

        #region Constructors

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

        #endregion

        #region Static Methods

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
        /// Prompts the user to select a template.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <returns>A template or null if the user cancelled.</returns>
        public static Template Prompt( Engine engine, string title )
        {
            string templateName = null;
            var templates = All().ToList();

            System.Windows.Application.Current.Dispatcher.Invoke( () =>
            {
                var dialog = new Dialogs.ComboBoxInputDialog( null, title ?? "Select Template" )
                {
                    Items = templates,
                    Required = true
                };

                if ( dialog.ShowDialog() == false )
                {
                    templateName = null;
                }
                else
                {
                    templateName = dialog.SelectedValue;
                }
            } );

            if ( templateName == null )
            {
                return null;
            }

            return new Template( engine, templateName );
        }

        #endregion

        #region Public Methods

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

        #endregion
    }
}
