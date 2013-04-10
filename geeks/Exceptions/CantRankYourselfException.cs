using System;

namespace geeks.Exceptions
{
    public class CantRankYourselfException : Exception
    {
        public CantRankYourselfException()
            : base("You are not allow to rate yourself! We assume you think you're pretty OK")
        {
        }
    }
}