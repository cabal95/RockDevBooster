using System;

using com.blueboxmoon.RockDevBooster.Bootstrap.Objects;
using Jint;

namespace com.blueboxmoon.RockDevBooster.Bootstrap
{
    public partial class Bootstrapper
    {
        #region Public Properties

        /// <summary>
        /// Notification that a message is to be logged.
        /// </summary>
        public event EventHandler<string> LogMessage;

        #endregion

        #region Javascript Methods

        protected void Log( object message )
        {
            LogMessage?.Invoke( this, message.ToString() + Environment.NewLine );
        }

        protected void LogProgress( object message )
        {
            LogMessage?.Invoke( this, "\r" + message.ToString() );
        }

        #endregion

        public void Execute( string script )
        {
            var engine = new Engine();

            engine.SetValue( "Template", EngineTypeReference.CreateTypeReference( engine, typeof( Template ) ) );
            engine.SetValue( "Instance", EngineTypeReference.CreateTypeReference( engine, typeof( Instance ) ) );
            engine.SetValue( "Log", new Action<object>( Log ) );
            engine.SetValue( "LogProgress", new Action<object>( LogProgress ) );

            engine.Execute( script );
        }
    }
}
