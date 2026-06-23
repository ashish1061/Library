using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Operations.Service.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            // For production, we can enforce JWT or cookie auth here.
            // But usually Hangfire runs on the same server, so we can allow local requests:
            // return httpContext.Request.IsLocal();
            
            // For now, allow all requests for development purposes (or implement custom auth logic).
            return true;
        }
    }
}
