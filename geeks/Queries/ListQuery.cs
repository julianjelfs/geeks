using System;
using System.Collections.Generic;
using System.Linq;

namespace geeks.Queries
{
    public abstract class ListQuery<T> : Query<ListResult<T>>
    {
        public int PageSize { get; set; }
        public int PageIndex { get; set; }

        protected ListResult<T> PageFrom(IEnumerable<T> list)
        {
            return new ListResult<T>
                {
                    TotalPages = (int)Math.Ceiling((double)list.Count() / PageSize),
                    List = list.Skip(PageIndex * PageSize).Take(PageSize)
                };
        }
    }
}