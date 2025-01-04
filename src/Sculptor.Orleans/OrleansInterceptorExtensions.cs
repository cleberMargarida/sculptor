using Microsoft.Extensions.DependencyInjection;
using Orleans;

namespace Sculptor.Orleans
{
    /// <summary>
    /// Provides extension methods for adding Orleans interceptors to the service collection.
    /// </summary>
    public static class OrleansInterceptorExtensions
    {
        /// <summary>
        /// Adds Orleans interceptors to the specified service collection.
        /// </summary>
        /// <param name="services">The service collection to add the interceptors to.</param>
        /// <returns>The service collection with the interceptors added.</returns>
        public static IServiceCollection AddOrleansInterceptors(this IServiceCollection services)
        {
            services.AddSingleton<IIncomingGrainCallFilter, SculptorIncomingGrainCallFilter>();
            return services;
        }
    }
}
