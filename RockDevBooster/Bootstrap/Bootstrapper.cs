using System;

using com.blueboxmoon.RockDevBooster.Bootstrap.Objects;
using Jint;

namespace com.blueboxmoon.RockDevBooster.Bootstrap
{
    /// <summary>
    /// Allows for running JavaScript scripts that allow for automated testing.
    /// </summary>
    public partial class Bootstrapper
    {
        #region Public Properties

        /// <summary>
        /// Notification that a message is to be logged.
        /// </summary>
        public event EventHandler<string> LogMessage;

        /// <summary>
        /// Gets or sets the execute started date time.
        /// </summary>
        /// <value>
        /// The execute started date time.
        /// </value>
        public DateTime ExecuteStartedDateTime { get; private set; }

        #endregion

        #region Javascript Methods

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Log( object message )
        {
            LogMessage?.Invoke( this, message.ToString() + Environment.NewLine );
        }

        /// <summary>
        /// Logs the progress, a progress message is one that replaces the last line
        /// of text.
        /// </summary>
        /// <param name="message">The message.</param>
        protected void LogProgress( object message )
        {
            LogMessage?.Invoke( this, "\r" + message.ToString() );
        }

        /// <summary>
        /// Aborts the script and logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="com.blueboxmoon.RockDevBooster.Bootstrap.Bootstrapper.EngineAbortException"></exception>
        protected void Abort( object message = null )
        {
            if ( message != null )
            {
                Log( message );
            }

            throw new EngineAbortException();
        }

        /// <summary>
        /// Beeps the computer.
        /// </summary>
        protected void Beep()
        {
            System.Media.SystemSounds.Beep.Play();
        }

        #endregion

        /// <summary>
        /// Executes the specified script.
        /// </summary>
        /// <param name="script">The script.</param>
        public void Execute( string script )
        {
            var engine = new Engine();

            ExecuteStartedDateTime = DateTime.Now;

            //
            // Add in this object for specialized use by other methods that
            // need to access us.
            //
            engine.SetValue( "__Bootstrap", this );

            //
            // Add in our global class types.
            //
            engine.SetValue( "Template", EngineTypeReference.CreateTypeReference( engine, typeof( Template ) ) );
            engine.SetValue( "Instance", EngineTypeReference.CreateTypeReference( engine, typeof( Instance ) ) );

            //
            // Add in global methods available to the scripts.
            //
            engine.SetValue( "Log", new Action<object>( Log ) );
            engine.SetValue( "LogProgress", new Action<object>( LogProgress ) );
            engine.SetValue( "Abort", new Action<object>( Abort ) );
            engine.SetValue( "Beep", new Action( Beep ) );

            try
            {
                engine.Execute( script );
            }
            catch ( EngineAbortException )
            {
                /* Intentionally left blank */
            }
            catch ( Exception ex )
            {
                var node = engine.GetLastSyntaxNode();

                if ( node != null && node.Location != null )
                {
                    Log( string.Format( "Exception occurred at line {0}.", node.Location.Start.Line ) );
                }
                throw ex;
            }
        }

        internal class EngineAbortException : Exception
        {
        }
    }
}
