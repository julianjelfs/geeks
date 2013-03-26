using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using geeks.Controllers;

namespace geeks
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapHttpRoute(
                name: "API Default",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            var appXmlType = GlobalConfiguration.Configuration.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
            GlobalConfiguration.Configuration.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);

            routes.MapRoute(
                name: "Default2",
                url: "event/{id}/{userId}",
                defaults: new { controller = "Home", action = "event" }
                );

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