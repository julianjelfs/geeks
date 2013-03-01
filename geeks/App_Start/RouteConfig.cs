using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace geeks
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "friends",
                url: "home/friends/{pageIndex}/{pageSize}",
                defaults: new { pageIndex = UrlParameter.Optional, pageSize = UrlParameter.Optional }
            );
            
            routes.MapRoute(
                name: "delete",
                url: "home/deletefriend/{id}/{pageIndex}/{pageSize}",
                defaults: new { pageIndex = UrlParameter.Optional, pageSize = UrlParameter.Optional }
            );
            
            /*routes.MapRoute(
                name: "hostname",
                url: "{host}/{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
            
            routes.MapRoute(
                name: "friends",
                url: "{host/friends/{pageIndex}/{pageSize}",
                defaults: new { pageIndex = 0, pageSize = 10 }
            );
            
            routes.MapRoute(
                name: "deletefriend",
                url: "home/deletefriend/{pageIndex}/{pageSize}",
                defaults: new { pageIndex = 0, pageSize = 10 }
            );*/
        }
    }
}