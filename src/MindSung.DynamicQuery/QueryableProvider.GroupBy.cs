using System;
using System.Linq;
using System.Linq.Expressions;


namespace MindSung.DynamicQuery
{
  public partial class QueryableProvider
  {
    protected virtual object InvokeGroupBy<T>(IQueryable<T> source, Expression groupKeyExpression, Type groupKeyType)
    {
      return source.GroupBy((Expression<Func<T, object>>)groupKeyExpression);
    }

    protected object InvokeGroupByExtensionMethod<T>(IQueryable<T> source, Expression groupKeyExpression, Type groupKeyType, Type extensionsType, string methodName = "GroupBy")
    {
      var groupMethods = extensionsType.GetMethods().Where(m => m.Name == methodName && m.IsStatic && m.IsGenericMethod &&
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
        .MakeGenericMethod(new [] { typeof(T), groupKeyType });
      return groupMethod.Invoke(null, new object[] { source, groupKeyExpression });
    }

    public IQueryable<object> GroupBy<T>(IQueryable<T> source, IQuery query)
    {
      // i
      var lambdaParam = Expression.Parameter(typeof(T), "i");
      // i => new { prop1 = i.prop1, ... }
      var typeExpr = ExpressionHelpers.InitializeTypeExpression(query.GroupBy, lambdaParam);
      var groupKeyType = typeExpr.Type;
      var delegateType = typeof(Func<,>).MakeGenericType(new [] { typeof(T), groupKeyType });
      // var lambda = Expression.Lambda<Func<T, object>>(typeExpr, lambdaParam);
      var lambda = (Expression)typeof(Expression).GetMethods()
        .Where(m => m.Name == "Lambda" && m.IsStatic && m.IsGenericMethod)
        .Select(m => new { m, targs = m.GetGenericArguments(), parms = m.GetParameters() })
        .Where(m => m.targs.Length == 1 && m.parms.Length == 2 &&
          m.parms[0].ParameterType == typeof(Expression) && m.parms[1].ParameterType == typeof(ParameterExpression[]))
        .First().m
        .MakeGenericMethod(new [] { delegateType })
        .Invoke(null, new object[] { typeExpr, new [] { lambdaParam } });

      // return source.GroupBy<T, object>(lambda);
      return GroupSelect<T>(InvokeGroupBy(source, lambda, groupKeyType), groupKeyType, query.GroupBy.Union(query.Select ?? new string[] { }).ToArray(), typeof(Queryable));
    }

    private IQueryable<object> GroupSelect<T>(object source, Type keyType, string[] selectProps, Type queryableExtensionsType)
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
