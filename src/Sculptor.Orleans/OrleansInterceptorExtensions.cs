using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Sculptor.Core;

namespace Sculptor.Orleans
{
    /// <summary>
    /// Extension methods for configuring the SiloBuilder.
    /// </summary>
    public static class SiloBuilderExtensions
    {
        /// <summary>
        /// Adds a filter to the incoming grain call pipeline that sets the ServiceProvider for the current context.
        /// </summary>
        /// <param name="builder">The ISiloBuilder to configure.</param>
        /// <returns>The configured ISiloBuilder.</returns>
        public static ISiloBuilder AddRichModelIncomingGrainCallFilter(this ISiloBuilder builder) => builder.AddIncomingGrainCallFilter(static context =>
        {
            ServiceProviderAccessor.ServiceProvider = context.TargetContext.ActivationServices;
            return context.Invoke();
        });
    }
}
