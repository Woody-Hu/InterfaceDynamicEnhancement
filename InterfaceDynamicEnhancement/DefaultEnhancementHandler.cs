using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace InterfaceDynamicEnhancement
{
    public class DefaultEnhancementHandler<T> : IEnhancementHandler<T>
    {
        private const string DynamicModuleName = "dynamicModule";

        private Lazy<IList<Assembly>> _assemblies = new Lazy<IList<Assembly>>(() => AppDomain.CurrentDomain.GetAssemblies());

        private readonly ConcurrentDictionary<Type, object> _concurrentDictionaryObjects = new ConcurrentDictionary<Type, object>();

        private readonly ConcurrentDictionary<string, Type> _concurrentDictionaryProxyTypes = new ConcurrentDictionary<string, Type>();

        private readonly ConcurrentDictionary<Type, Type> _concurrentDictionaryAppendObjectTypes = new ConcurrentDictionary<Type, Type>();

        private readonly string _dynamicAssembly = Guid.NewGuid().ToString();

        private readonly Lazy<ModuleBuilder> _moduleBuilder;

        public DefaultEnhancementHandler()
        {
            var assemblyBuilder = new Lazy<AssemblyBuilder>(() => AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(_dynamicAssembly), AssemblyBuilderAccess.Run));
            _moduleBuilder = new Lazy<ModuleBuilder>(() => assemblyBuilder.Value.DefineDynamicModule(DynamicModuleName));
        }

        public Task<T2> EnhancementObjectAsync<T2>(T coreObject, bool singleton) where T2 : T
        {
            if (singleton && _concurrentDictionaryObjects.ContainsKey(typeof(T2)))
            {
                return Task.FromResult((T2) _concurrentDictionaryObjects[typeof(T2)]);
            }
            throw new NotImplementedException();
        }

        public Task<T2> EnhancementObjectAsync<T2>(T coreObject, object appendObject) where T2 : T
        {
            var key = $"{typeof(T2).FullName};{appendObject.GetType().FullName}";
            var t = _concurrentDictionaryProxyTypes.GetOrAdd(key, CreateProxyType<T2>(appendObject.GetType())) ;
            var res = (T2)Activator.CreateInstance(t, coreObject, appendObject);
            return Task.FromResult(res);
        }

        private Type CreateProxyType<T2> (Type appendType) where T2 : T
        {
            var typeName = typeof(T2).Name + Guid.NewGuid();
            var typeBuilder = _moduleBuilder.Value.DefineType(typeName, TypeAttributes.Class | TypeAttributes.Public, typeof(object));
            typeBuilder.AddInterfaceImplementation(typeof(T2));
            var coreObjectFiled = typeBuilder.DefineField("_coreObject", typeof(T), FieldAttributes.Private);
            var appendObjectFiled = typeBuilder.DefineField("_appendObject", appendType, FieldAttributes.Private);
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(T), appendType });
            var constructorIlo = constructorBuilder.GetILGenerator();
            constructorIlo.Emit(OpCodes.Ldarg_0);
            constructorIlo.Emit(OpCodes.Ldarg_1);
            constructorIlo.Emit(OpCodes.Stfld, coreObjectFiled);
            constructorIlo.Emit(OpCodes.Ldarg_0);
            constructorIlo.Emit(OpCodes.Ldarg_2);
            constructorIlo.Emit(OpCodes.Stfld, appendObjectFiled);
            constructorIlo.Emit(OpCodes.Ret);

            var methodsOfT = typeof(T).GetMethods();
            var methodsOfT2 = typeof(T2).GetMethods();
            foreach (var oneMethod in methodsOfT)
            {
                CreateProxyMethod<T2>(oneMethod, typeBuilder, coreObjectFiled);
            }

            foreach (var oneMethod in methodsOfT2)
            {
                var appendObjectType = appendType;
                var methodParameterTypes = oneMethod.GetParameters().Select(k => k.ParameterType).ToArray();
                var appendObjectMethod = appendObjectType.GetMethod(oneMethod.Name, methodParameterTypes);
                if (appendObjectMethod == null || appendObjectMethod.ReturnType != oneMethod.ReturnType)
                {
                    continue;
                }

                var methodBuilder = typeBuilder.DefineMethod(oneMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.Standard, oneMethod.ReturnType, methodParameterTypes);
                var methodIlo = methodBuilder.GetILGenerator();
                methodIlo.Emit(OpCodes.Ldarg_0);
                methodIlo.Emit(OpCodes.Ldfld, appendObjectFiled);
                for (int i = 0; i < methodParameterTypes.Length; i++)
                {
                    methodIlo.Emit(OpCodes.Ldarg_S, i + 1);
                }

                methodIlo.Emit(OpCodes.Call, appendObjectMethod);
                methodIlo.Emit(OpCodes.Ret);
            }

            var t = typeBuilder.CreateType();
            return t;
        }

        private void CreateProxyMethod<T2>(MethodInfo oneMethod, TypeBuilder typeBuilder, FieldBuilder objectFieldBuilder)
            where T2 : T
        {
            var methodParameterTypes = oneMethod.GetParameters().Select(k => k.ParameterType).ToArray();
            var methodBuilder = typeBuilder.DefineMethod(oneMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual,
                CallingConventions.Standard, oneMethod.ReturnType, methodParameterTypes);
            var methodIlo = methodBuilder.GetILGenerator();
            methodIlo.Emit(OpCodes.Ldarg_0);
            methodIlo.Emit(OpCodes.Ldfld, objectFieldBuilder);
            for (int i = 0; i < methodParameterTypes.Length; i++)
            {
                methodIlo.Emit(OpCodes.Ldarg_S, i + 1);
            }

            methodIlo.Emit(OpCodes.Call, oneMethod);
            methodIlo.Emit(OpCodes.Ret);
        }
    }
}
