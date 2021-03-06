﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.Windsor;
using Castle.Windsor.Installer;
using SWTOR.Web.IoC;
using SWTOR.Web.Filters;
using Castle.Core.Logging;
using System.Configuration;

namespace SWTOR.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static IWindsorContainer container;

        private static void BootstrapContainer()
        {
            container = new WindsorContainer();     // Create a container to hold the dependencies
            var controllerFactory = new WindsorControllerFactory(container.Kernel);     // Create a new instance
            ControllerBuilder.Current.SetControllerFactory(controllerFactory);          // Use my factory instead of default

            container.Install(
                FromAssembly.This()
            );      // Scan this assembly for all IWindsorInstaller

        }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            BootstrapContainer();
            LogConnectionInfo();
            AreaRegistration.RegisterAllAreas();

            GlobalFilters.Filters.Add(container.Resolve<ErrorLoggerAttribute>());

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }
        private void LogConnectionInfo()
        {
            var logger = MvcApplication.container.Resolve<ILogger>();
            logger.InfoFormat("Raven Connection Info : {0}", ConfigurationManager.ConnectionStrings["RavenDB"].ConnectionString);
        }
    }
}