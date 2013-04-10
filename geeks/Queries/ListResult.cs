using System.Collections.Generic;

namespace geeks.Queries
{
    public class ListResult<T>
    {
        public IEnumerable<T> List { get; set; }
        public int TotalPages { get; set; }
    }
}