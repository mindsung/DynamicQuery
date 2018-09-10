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
}