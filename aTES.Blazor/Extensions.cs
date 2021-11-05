using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Specialized;
using System.Web;

namespace aTES.Blazor
{
    public static class Extensions
    {
        public static void AddPopugBlazor(this IServiceCollection services)
        {
            services.AddScoped<IAlertService, AlertService>();
        }

        public static NameValueCollection QueryString(this NavigationManager navigationManager)
        {
            return HttpUtility.ParseQueryString(new Uri(navigationManager.Uri).Query);
        }

        public static string QueryString(this NavigationManager navigationManager, string key)
        {
            return navigationManager.QueryString()[key];
        }
    }
}
