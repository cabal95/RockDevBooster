using System;

using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace com.blueboxmoon.RockDevBooster.Bootstrap
{
    /// <summary>
    /// A custom version of the TypeReference class that handles constructors with
    /// a first parameter of type Engine.
    /// </summary>
    /// <seealso cref="Jint.Native.Function.FunctionInstance" />
    /// <seealso cref="Jint.Native.IConstructor" />
    /// <seealso cref="Jint.Runtime.Interop.IObjectWrapper" />
    public class EngineTypeReference : FunctionInstance, IConstructor, IObjectWrapper
    {
        /// <summary>
        /// The original type reference
        /// </summary>
        private TypeReference _typeReference;

        /// <summary>
        /// Gets or sets the type of this reference.
        /// </summary>
        /// <value>
        /// The type of this reference.
        /// </value>
        public Type Type { get; set; }

        /// <summary>
        /// Gets the target.
        /// </summary>
        /// <value>
        /// The target.
        /// </value>
        public object Target
        {
            get
            {
                return Type;
            }
        }

        /// <summary>
        /// Gets the class name.
        /// </summary>
        /// <value>
        /// The class name.
        /// </value>
        public override string Class
        {
            get { return "EngineTypeReference"; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineTypeReference"/> class.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="typeReference">The original type reference.</param>
        private EngineTypeReference( Engine engine, TypeReference typeReference )
            : base( engine, null, null, false )
        {
            _typeReference = typeReference;
        }

        /// <summary>
        /// Creates the type reference.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static EngineTypeReference CreateTypeReference( Engine engine, Type type )
        {
            var obj = new EngineTypeReference( engine, TypeReference.CreateTypeReference( engine, type ) )
            {
                Extensible = false,
                Type = type,
                Prototype = engine.Function.PrototypeObject
            };

            obj.FastAddProperty( "length", 0, false, false, false );

            // The initial value of Boolean.prototype is the Boolean prototype object
            obj.FastAddProperty( "prototype", engine.Object.PrototypeObject, false, false, false );

            return obj;
        }

        /// <summary>
        /// Calls this object.
        /// </summary>
        /// <param name="thisObject">The this object.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns></returns>
        public override JsValue Call( JsValue thisObject, JsValue[] arguments )
        {
            // direct calls on a TypeReference constructor object is equivalent to the new operator
            return Construct( arguments );
        }

        /// <summary>
        /// Constructs the original type with the specified arguments.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        /// <returns></returns>
        public ObjectInstance Construct( JsValue[] arguments )
        {
            try
            {
                JsValue[] newArguments = new JsValue[arguments.Length + 1];

                newArguments[0] = JsValue.FromObject( Engine, Engine );
                arguments.CopyTo( newArguments, 1 );

                return _typeReference.Construct( newArguments );
            }
            catch ( JavaScriptException ex )
            {
                if ( ex.Message.Contains( "No public methods" ) )
                {
                    return _typeReference.Construct( arguments );
                }

                throw ex;
            }
        }

        /// <summary>
        /// Defines a property on our self.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="desc">The desc.</param>
        /// <param name="throwOnError">if set to <c>true</c> [throw on error].</param>
        /// <returns></returns>
        /// <exception cref="JavaScriptException">Can't define a property of a TypeReference</exception>
        public override bool DefineOwnProperty( string propertyName, PropertyDescriptor desc, bool throwOnError )
        {
            if ( throwOnError )
            {
                throw new JavaScriptException( Engine.TypeError, "Can't define a property of a TypeReference" );
            }

            return false;
        }

        /// <summary>
        /// Deletes the specified property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="throwOnError">if set to <c>true</c> [throw on error].</param>
        /// <returns></returns>
        /// <exception cref="JavaScriptException">Can't delete a property of a TypeReference</exception>
        public override bool Delete( string propertyName, bool throwOnError )
        {
            if ( throwOnError )
            {
                throw new JavaScriptException( Engine.TypeError, "Can't delete a property of a TypeReference" );
            }

            return false;
        }

        /// <summary>
        /// Puts the specified property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        /// <param name="throwOnError">if set to <c>true</c> [throw on error].</param>
        public override void Put( string propertyName, JsValue value, bool throwOnError )
        {
            _typeReference.Put( propertyName, value, throwOnError );
        }

        /// <summary>
        /// Gets a property that is defined on our type.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public override PropertyDescriptor GetOwnProperty( string propertyName )
        {
            var pd = _typeReference.GetOwnProperty( propertyName );

            if ( pd != null && pd.Value.IsObject() && pd.Value.AsObject() is MethodInfoFunctionInstance )
            {
                return new PropertyDescriptor( new MethodInfoEngineFunctionInstance( Engine, ( MethodInfoFunctionInstance ) pd.Value.AsObject() ), false, false, false );
            }

            return pd;
        }
    }
}
