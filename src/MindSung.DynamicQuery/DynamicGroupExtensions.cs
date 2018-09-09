using System;
using System.Linq;
using System.Linq.Expressions;

namespace MindSung.DynamicQuery
{
  internal static class DynamicGroupExtensions
  {
    public static IQueryable<object> DynamicGroupBy<T>(this IQueryable<T> source, string[] groupProps, string[] selectProps, Type queryableExtensionsType, out Type resultType)
    {
      // i
      var lambdaParam = Expression.Parameter(typeof(T), "i");
      // i => new { prop1 = i.prop1, ... }
      var typeExpr = ExpressionHelpers.InitializeTypeExpression(groupProps, lambdaParam);
      resultType = typeExpr.Type;
      var delegateType = typeof(Func<,>).MakeGenericType(new [] { typeof(T), resultType });
      // var lambda = Expression.Lambda<Func<T, object>>(typeExpr, lambdaParam);
      var lambda = typeof(Expression).GetMethods()
        .Where(m => m.Name == "Lambda" && m.IsStatic && m.IsGenericMethod)
        .Select(m => new { m, targs = m.GetGenericArguments(), parms = m.GetParameters() })
        .Where(m => m.targs.Length == 1 && m.parms.Length == 2 &&
          m.parms[0].ParameterType == typeof(Expression) && m.parms[1].ParameterType == typeof(ParameterExpression[]))
        .First().m
        .MakeGenericMethod(new [] { delegateType })
        .Invoke(null, new object[] { typeExpr, new [] { lambdaParam } });

      var groupMethods = queryableExtensionsType.GetMethods().Where(m => m.Name == "GroupBy" && m.IsStatic && m.IsGenericMethod &&
          m.GetParameters().Length == 2)
        .Where(m =>
        {
          var args = m.GetGenericArguments();
          if (args.Length == 2)
          {
            return true;
          }
          return false;
        }).ToList();
      var groupMethod = groupMethods.First()
        .MakeGenericMethod(new [] { typeof(T), resultType });
      var groupResult = groupMethod.Invoke(null, new object[] { source, lambda });

      //return source.GroupBy<T, object>(lambda);
      return DynamicGroupSelect<T>(groupResult, resultType, groupProps.Union(selectProps ?? new string[] { }).ToArray(), queryableExtensionsType);
    }

    public static IQueryable<object> DynamicGroupSelect<T>(object source, Type keyType, string[] selectProps, Type queryableExtensionsType)
    {
      // g
      var groupingType = typeof(IGrouping<,>).MakeGenericType(new [] { keyType, typeof(T) });
      var lambdaParam = Expression.Parameter(groupingType, "g");
      // g => new { prop1 = g.Key.prop1, ... }
      var typeExpr = ExpressionHelpers.InitializeTypeExpression(
        selectProps.Select(p => "Key." + p).ToArray(), lambdaParam, true);

      var resultType = typeExpr.Type;
      var funcType = typeof(Func<,>).MakeGenericType(new [] { groupingType, resultType });
      var lambdaMethods = typeof(Expression).GetMethods()
        .Where(m => m.Name == "Lambda" && m.IsStatic && m.IsGenericMethod && m.GetParameters().Length == 2)
        .Where(m =>
        {
          var args = m.GetGenericArguments();
          if (args.Length == 1)
          {
            var args2 = args[0].GetGenericArguments();
            return true;
          }
          return false;
        })
        .ToList();
      var lambdaMethod = lambdaMethods.First()
        .MakeGenericMethod(new [] { funcType });;
      var lambda = (Expression)lambdaMethod.Invoke(null, new object[] { typeExpr, new [] { lambdaParam } });
      //var lambda = Expression.Lambda<Func<T, object>>(typeExpr, lambdaParam);
      var selectMethods = queryableExtensionsType.GetMethods().Where(m => m.Name == "Select" && m.IsStatic && m.IsGenericMethod &&
          m.GetParameters().Length == 2)
        .Where(m =>
        {
          var args = m.GetGenericArguments();
          if (args.Length == 2)
          {
            return true;
          }
          return false;
        }).ToList();
      var selectMethod = selectMethods.First()
        .MakeGenericMethod(new [] { groupingType, resultType });
      return (IQueryable<object>)selectMethod.Invoke(null, new object[] { source, lambda });
    }
  }
}