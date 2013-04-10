using Raven.Client;

namespace geeks.Queries
{
    public abstract class Query<T>
    {
        public IDocumentSession Session { get; set; }
        public string CurrentUserId { get; set; }

        public abstract T Execute();

        protected T ExecuteQuery<T>(Query<T> query)
        {
            query.CurrentUserId = CurrentUserId;
            query.Session = Session;
            return query.Execute();
        }
    }
}