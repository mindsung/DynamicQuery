using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MindSung.DynamicQuery
{
  public static class DynamicQueryableExtensions
  {
    private static Type defaultQueryableExtensionsType = typeof(Queryable);

    private static IQueryable<object> DynamicQuery<T>(this IQueryable<T> source, string queryString, Type queryableExtensionsType, out Type resultType)
    {
      queryableExtensionsType = queryableExtensionsType ?? defaultQueryableExtensionsType;
      var query = new Query(queryString);
      return source.DynamicGroupBy(query.GroupBy, query.Select, queryableExtensionsType, out resultType);
    }

    public static IQueryable<object> DynamicQuery<T>(this IQueryable<T> source, string queryString, Type queryableExtensionsType)
    {
      return DynamicQuery(source, queryString, queryableExtensionsType, out var _);
    }

    public static IQueryable<object> DynamicQuery<T>(this IQueryable<T> source, string queryString)
    {
      return DynamicQuery(source, queryString, null, out var _);
    }

    public static Task<IEnumerable<object>> DynamicQueryResolveAsync<T>(this IQueryable<T> source, string queryString, Type queryableExtensionsType, AsyncResolver resolver, params object[] args)
    {
      return resolver.ResolveAsync(DynamicQuery(source, queryString, queryableExtensionsType, out var resultType), resultType, args);
    }
  }

  public static class DynamicEnumerableExtensions
  {
    public static IQueryable<object> DynamicQuery<T>(this IEnumerable<T> enumerable, string queryString)
    {
      var query = new Query(queryString);
      return enumerable.AsQueryable().DynamicQuery(queryString);
    }
  }
}