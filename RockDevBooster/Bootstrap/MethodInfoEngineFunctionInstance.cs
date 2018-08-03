using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace com.blueboxmoon.RockDevBooster.Bootstrap
{
    /// <summary>
    /// A custom version of the MethodInfoFunctionInstance class that handles
    /// methods that take an Engine as the first parameter.
    /// </summary>
    /// <seealso cref="Jint.Native.Function.FunctionInstance" />
    public class MethodInfoEngineFunctionInstance : FunctionInstance
    {
        /// <summary>
        /// The original instance
        /// </summary>
        private MethodInfoFunctionInstance _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodInfoEngineFunctionInstance"/> class.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="instance">The origina method info instance.</param>
        public MethodInfoEngineFunctionInstance( Engine engine, MethodInfoFunctionInstance instance )
            : base( engine, null, null, false )
        {
            _instance = instance;
        }

        /// <summary>
        /// Calls the method on the original instance.
        /// </summary>
        /// <param name="thisObject">The this object.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns></returns>
        public override JsValue Call( JsValue thisObject, JsValue[] arguments )
        {
            try
            {
                JsValue[] newArguments = new JsValue[arguments.Length + 1];

                newArguments[0] = JsValue.FromObject( Engine, Engine );
                arguments.CopyTo( newArguments, 1 );

                return _instance.Call( thisObject, newArguments );
            }
            catch ( JavaScriptException ex )
            {
                if ( ex.Message.Contains( "No public methods" ) )
                {
                    return _instance.Call( thisObject, arguments );
                }

                throw ex;
            }
        }
    }
}
