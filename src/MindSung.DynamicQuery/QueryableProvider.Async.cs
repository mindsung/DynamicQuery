using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MindSung.DynamicQuery
{
  public partial class QueryableProvider
  {
    private static ConcurrentDictionary<Type, ConcurrentDictionary<string, ConcurrentDictionary<Type, MethodInfo>>> asyncMethods
      = new ConcurrentDictionary<Type, ConcurrentDictionary<string, ConcurrentDictionary<Type, MethodInfo>>>();

    protected async Task<TCast> InvokeAsyncExtensionMethod<TCast>(IQueryable<object> source, Type extensionsType, string methodName, object[] args)
    {
      var allArgs = new object[args.Length + 1];
      allArgs[0] = source;
      Array.Copy(args, 0, allArgs, 1, args.Length);
      dynamic resultAwaitable = asyncMethods
        .GetOrAdd(extensionsType, _ => new ConcurrentDictionary<string, ConcurrentDictionary<Type, MethodInfo>>())
        .GetOrAdd(methodName, _ => new ConcurrentDictionary<Type, MethodInfo>())
        .GetOrAdd(source.ElementType, _ => extensionsType.GetMethod(methodName).MakeGenericMethod(source.ElementType))
        .Invoke(null, allArgs);
      await resultAwaitable.ConfigureAwait(false);
      return resultAwaitable.GetAwaiter().GetResult();
    }
  }
}
