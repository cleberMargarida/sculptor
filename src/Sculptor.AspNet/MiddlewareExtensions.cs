using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Sculptor.Core;

namespace Sculptor.AspNet
{
    /// <summary>
    /// Contains extension methods for adding the Sculptor middleware.
    /// </summary>
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Adds the Sculptor middleware to the application's request pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance with the Sculptor middleware added.</returns>
        public static IApplicationBuilder UseSculptor(this IApplicationBuilder builder) => builder.Use(static (
            HttpContext context, 
            RequestDelegate next) =>
            {
                ServiceProviderAccessor.ServiceProvider = context.RequestServices;
                return next(context);
            });
    }
}
