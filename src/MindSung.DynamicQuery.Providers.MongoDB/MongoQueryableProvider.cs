using System;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver.Linq;
using MindSung.DynamicQuery;

namespace MindSung.DynamicQuery.Providers.MongoDB
{
  public class MongoQueryableProvider : QueryableProvider
  {
    public static readonly MongoQueryableProvider MongoDefaultProvider = new MongoQueryableProvider();

    protected override object InvokeGroupBy<T>(IQueryable<T> source, Expression groupKeyExpression, Type groupKeyType)
    {
      return InvokeGroupByExtensionMethod(source, groupKeyExpression, groupKeyType, typeof(MongoQueryable));
    }
  }
}