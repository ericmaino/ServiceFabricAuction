using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dependencies;
using Owin;

namespace SFAuction.Svc.Auction
{
    internal static class WebAppStart
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public static void ConfigureApp(IAppBuilder appBuilder, string rootPrefix = null, Func<IDependencyResolver, IDependencyResolver> resolver = null)
        {
            // Configure Web API for self-host.
            HttpConfiguration config = new HttpConfiguration();

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });

            appBuilder.UseWebApi(config);

            if (resolver != null)
            {
                config.DependencyResolver = resolver(config.DependencyResolver);
            }
        }
    }
}
