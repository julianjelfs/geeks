using System;
using System.Web.Mvc;
using log4net;

namespace geeks.Controllers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class HandleErrorAsHttpAttribute : FilterAttribute, IExceptionFilter
    {
        private ILog _logger = LogManager.GetLogger("Geeks");

        public void OnException(ExceptionContext filterContext)
        {
            _logger.Error(filterContext.Exception.Message, filterContext.Exception);

            filterContext.Result = new HttpStatusCodeResult(500, filterContext.Exception.Message);
            filterContext.ExceptionHandled = true;
            filterContext.HttpContext.Response.Clear();
            filterContext.HttpContext.Response.Write(filterContext.Exception.Message);
            filterContext.HttpContext.Response.StatusCode = 500;
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
        }
    }
}