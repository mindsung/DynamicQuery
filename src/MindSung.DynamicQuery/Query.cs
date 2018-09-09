using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MindSung.DynamicQuery
{
  public interface IQuery
  {
    string[] Select { get; }
    string[] Where { get; }
    string[] OrderBy { get; }
    string[] GroupBy { get; }
    int Skip { get; }
    int Take { get; }
  }

  public class Query : IQuery
  {
    public Query()
    { }

    public Query(string queryString)
    {
      // decode any URI escaped characters.
      var parts = new QueryStringParts(Uri.UnescapeDataString(queryString ?? ""));
      Select = parts["select"].SelectMany(s => s.Split(',')).ToArray();
      Where = parts["where"];
      OrderBy = parts["orderby"].SelectMany(s => s.Split(',')).ToArray();
      GroupBy = parts["groupby"].SelectMany(s => s.Split(',')).ToArray();
      Skip = int.Parse(parts["skip"].FirstOrDefault() ?? "0");
      Take = int.Parse(parts["take"].FirstOrDefault() ?? "0");
    }

    public string[] Select { get; set; }
    public string[] Where { get; set; }
    public string[] OrderBy { get; set; }
    public string[] GroupBy { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }

    class QueryStringParts
    {
      public QueryStringParts(string queryString)
      {
        if (!string.IsNullOrWhiteSpace(queryString))
        {
          var iqs = queryString.IndexOf('?');
          if (iqs >= 0)queryString = queryString.Substring(iqs + 1);
          dict = new Dictionary<string, string[]>(queryString
            .Split('&').Select(p => p.Split(new [] { '=' }, 2))
            .GroupBy(kv => kv[0])
            .ToDictionary(g => g.Key, g => g.SelectMany(v => v.Skip(1)).ToArray()),
            StringComparer.OrdinalIgnoreCase);
        }
        else
        {
          dict = new Dictionary<string, string[]>();
        }
      }

      private Dictionary<string, string[]> dict;

      public string[] this[string key] => dict.TryGetValue(key, out var v) ? v : new string[0];
    }
  }
}