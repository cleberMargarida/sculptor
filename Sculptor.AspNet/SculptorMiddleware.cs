using Microsoft.AspNetCore.Http;
using Sculptor.Core;
using System.Threading.Tasks;

namespace Sculptor.AspNet
{
    internal class SculptorMiddleware : IMiddleware
    {
        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            ServiceProviderAccessor.ServiceProvider = context.RequestServices;
            return next(context);
        }
    }
}