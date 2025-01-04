using System;
using System.Threading;

namespace Sculptor.Core
{
    /// <summary>
    /// Represents the base class for all domain models.
    /// </summary>
    public abstract class RichModel
    {
        /// <summary>
        /// Gets the current <see cref="IServiceProvider"/> instance.
        /// Throws <see cref="NullReferenceException"/> if the ServiceProvider is not configured.
        /// </summary>
        protected static IServiceProvider Services => ServiceProviderAccessor.ServiceProvider 
            ?? throw new NullReferenceException("Sculptor had not configured the ServiceProvider. Please ensure that the ServiceProvider is properly set up before accessing it.");
    }

    public static class ServiceProviderAccessor
    {
        private static readonly AsyncLocal<ServiceProviderHolder> _serviceProviderCurrent = new();

        /// <summary>
        /// Gets or sets the current <see cref="IServiceProvider"/>.
        /// </summary>
        public static IServiceProvider? ServiceProvider
        {
            internal get
            {
                return _serviceProviderCurrent.Value?.Provider;
            }
            set
            {
                var holder = _serviceProviderCurrent.Value;
                if (holder != null)
                {
                    // Clear current ServiceProvider trapped in the AsyncLocals, as its done.
                    holder.Provider = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the ServiceProvider in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    _serviceProviderCurrent.Value = new ServiceProviderHolder { Provider = value };
                }
            }
        }

        /// <summary>
        /// Holds the <see cref="IServiceProvider"/> in an AsyncLocal context.
        /// </summary>
        private class ServiceProviderHolder
        {
            /// <summary>
            /// Gets or sets the <see cref="IServiceProvider"/>.
            /// </summary>
            public IServiceProvider? Provider;
        }
    }
}
