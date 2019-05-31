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
        private Lazy<IList<Assembly>> _assemblies = null;

        private readonly ConcurrentDictionary<Type, object> _concurrentDictionaryObjects = new ConcurrentDictionary<Type, object>();

        private static string _dynamicAssembly = Guid.NewGuid().ToString();

        private const string _dynamicModuleName = "dynamicModule";

        private static Lazy<AssemblyBuilder> _assemblyBuilder = new Lazy<AssemblyBuilder>(()=> AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(_dynamicAssembly) , AssemblyBuilderAccess.Run));

        private static Lazy<ModuleBuilder> _moduleBuilder = new Lazy<ModuleBuilder>(()=> _assemblyBuilder.Value.DefineDynamicModule(_dynamicModuleName));

        public Task<T2> EnhancementObjectAsync<T2>(T coreObject, bool singleton) where T2 : T
        {
            throw new NotImplementedException();
        }

        public T2 EnhancementObjectAsync<T2>(T coreObject, object appendObject) where T2 : T
        {
            var typeName = typeof(T2).Name + Guid.NewGuid();
            var typeBuilder = _moduleBuilder.Value.DefineType(typeName, TypeAttributes.Class | TypeAttributes.Public, typeof(object));
            typeBuilder.AddInterfaceImplementation(typeof(T2));
            var coreObjectFiled = typeBuilder.DefineField("_coreObject", typeof(T), FieldAttributes.Private);
            var appendObjectFiled = typeBuilder.DefineField("_appendObject", appendObject.GetType(), FieldAttributes.Private);
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] {typeof(T), appendObject.GetType()});
            var constructorIlo = constructorBuilder.GetILGenerator();
            constructorIlo.Emit(OpCodes.Ldarg_0);
            constructorIlo.Emit(OpCodes.Ldarg_1);
            constructorIlo.Emit(OpCodes.Stfld, coreObjectFiled);
            constructorIlo.Emit(OpCodes.Ldarg_0);
            constructorIlo.Emit(OpCodes.Ldarg_2);
            constructorIlo.Emit(OpCodes.Stfld, appendObjectFiled);
            constructorIlo.Emit(OpCodes.Ret);

            var methodsOfT = typeof(T).GetMethods();

            foreach (var oneMethod in methodsOfT)
            {
                var methodParameterTypes = oneMethod.GetParameters().Select(k => k.ParameterType).ToArray();
                var methodBuilder = typeBuilder.DefineMethod(oneMethod.Name, MethodAttributes.Public|MethodAttributes.Virtual, CallingConventions.Standard, oneMethod.ReturnType, methodParameterTypes);
                var methodIlo = methodBuilder.GetILGenerator();
                methodIlo.Emit(OpCodes.Ldarg_0);
                methodIlo.Emit(OpCodes.Ldfld, coreObjectFiled);
                for (int i = 0; i < methodParameterTypes.Length; i++)
                {
                    methodIlo.Emit(OpCodes.Ldarg_S, i + 1);
                }
                methodIlo.Emit(OpCodes.Call, oneMethod);
                methodIlo.Emit(OpCodes.Ret);
            }

            var t = typeBuilder.CreateType();
            var res = (T2)Activator.CreateInstance(t, coreObject, appendObject);
            return res;
        }
    }
}
