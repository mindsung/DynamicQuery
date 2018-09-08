using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MindSung.DynamicQuery
{
  public static class DynamicType
  {
    private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
    {
      FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

      PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
      MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
      ILGenerator getIl = getPropMthdBldr.GetILGenerator();

      getIl.Emit(OpCodes.Ldarg_0);
      getIl.Emit(OpCodes.Ldfld, fieldBuilder);
      getIl.Emit(OpCodes.Ret);

      MethodBuilder setPropMthdBldr =
        tb.DefineMethod("set_" + propertyName,
          MethodAttributes.Public |
          MethodAttributes.SpecialName |
          MethodAttributes.HideBySig,
          null, new [] { propertyType });

      ILGenerator setIl = setPropMthdBldr.GetILGenerator();
      Label modifyProperty = setIl.DefineLabel();
      Label exitSet = setIl.DefineLabel();

      setIl.MarkLabel(modifyProperty);
      setIl.Emit(OpCodes.Ldarg_0);
      setIl.Emit(OpCodes.Ldarg_1);
      setIl.Emit(OpCodes.Stfld, fieldBuilder);

      setIl.Emit(OpCodes.Nop);
      setIl.MarkLabel(exitSet);
      setIl.Emit(OpCodes.Ret);

      propertyBuilder.SetGetMethod(getPropMthdBldr);
      propertyBuilder.SetSetMethod(setPropMthdBldr);
    }

    private static int dynamicTypeId = 0;
    private static ConcurrentDictionary<string, Type> dynamicTypes = new ConcurrentDictionary<string, Type>();
    private static ModuleBuilder dynamicModuleBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("_dynamic"), AssemblyBuilderAccess.Run)
      .DefineDynamicModule("_dynamic");

    public static Type GetDynamicType((string name, Type type) [] props, bool createSubPropTypes = false)
    {
      return dynamicTypes.GetOrAdd(string.Join("+", props.OrderBy(p => p.name).Select(p => p.name + "=" + p.type)), _ =>
      {
        TypeBuilder tb = dynamicModuleBuilder.DefineType($"_dynamic.t_{Interlocked.Increment(ref dynamicTypeId)}", TypeAttributes.Public);
        var grouped = props.GroupBy(p => p.name.Split(new [] { '.' }, 2) [0]).Select(g => new { name = g.Key, subs = g.ToList() }).ToList();
        foreach (var p in grouped)
        {
          var pname = p.name;
          if (!createSubPropTypes || (p.subs.Count == 1 && p.subs[0].name.Length == pname.Length))
          {
            // No sub properties
            CreateProperty(tb, pname, p.subs[0].type);
          }
          else if (createSubPropTypes)
          {
            // Sub properties
            CreateProperty(tb, pname, GetDynamicType(p.subs.Select(s =>(s.name.Substring(p.name.Length + 1), s.type)).ToArray()));
          }
        }
        return tb.CreateTypeInfo().AsType();
      });
    }

    public static Type GetDynamicType(string[] propNames, bool createSubPropTypes = false)
    {
      return GetDynamicType(propNames.Select(n =>(n, typeof(object))).ToArray(), createSubPropTypes);
    }
  }
}