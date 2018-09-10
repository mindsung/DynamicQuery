using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;

namespace MindSung.DynamicQuery.Providers.MongoDB
{
  public static class MongoQueryableProviderExtensions
  {
    private static readonly MongoQueryableProvider provider = new MongoQueryableProvider();

    public static IQueryable<object> Query<T>(this IMongoQueryable<T> source, string queryString)
    {
      return provider.Query(source, queryString);
    }

    public static Task<IEnumerable<object>> ToListAsync(this IQueryable<object> source, CancellationToken cancellationToken = default(CancellationToken))
    {
      return provider.ToListAsync(source, cancellationToken);
    }
  }
}
