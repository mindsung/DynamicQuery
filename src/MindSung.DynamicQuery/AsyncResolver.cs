using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MindSung.DynamicQuery
{
  public class AsyncResolver
  {
    public AsyncResolver(Type asyncExtensionsType, string methodName)
    {
      this.asyncExtensionsType = asyncExtensionsType;
      this.methodName = methodName;
    }

    private Type asyncExtensionsType;
    private string methodName;

    private ConcurrentDictionary<Type, MethodInfo> asyncMethods = new ConcurrentDictionary<Type, MethodInfo>();

    internal async Task<IEnumerable<object>> ResolveAsync(IQueryable<object> source, Type resultType, object[] args)
    {
      var allArgs = new object[args.Length + 1];
      allArgs[0] = source;
      Array.Copy(args, 0, allArgs, 1, args.Length);
      dynamic resultAwaitable = asyncMethods.GetOrAdd(resultType,
          t => asyncExtensionsType.GetMethod(methodName).MakeGenericMethod(resultType))
        .Invoke(null, allArgs);
      await resultAwaitable.ConfigureAwait(false);
      return resultAwaitable.GetAwaiter().GetResult();
    }
  }
}