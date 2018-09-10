using System;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MindSung.DynamicQuery;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace MindSung.DynamicQuery.Providers.MongoDB
{
  public class MongoQueryableProvider : QueryableProvider
  {
    public static readonly MongoQueryableProvider MongoDefaultProvider = new MongoQueryableProvider();

    protected override object InvokeGroupBy<T>(IQueryable<T> source, Expression groupKeyExpression, Type groupKeyType)
    {
      return InvokeGroupByExtensionMethod(source, groupKeyExpression, groupKeyType, typeof(MongoQueryable));
    }
    
    public async Task<IEnumerable<object>> ToListAsync(IQueryable<object> source, CancellationToken cancellationToken = default(CancellationToken))
    {
      return await InvokeAsyncExtensionMethod<IEnumerable<object>>(source, typeof(IAsyncCursorSourceExtensions), "ToListAsync", new object[] { cancellationToken });
    }
  }
}