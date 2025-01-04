using Orleans;
using Sculptor.Core;
using System.Threading.Tasks;

namespace Sculptor.Orleans
{
    internal class SculptorIncomingGrainCallFilter : IIncomingGrainCallFilter
    {
        public Task Invoke(IIncomingGrainCallContext context)
        {
            ServiceProviderAccessor.ServiceProvider = context.TargetContext.ActivationServices;
            return context.Invoke();
        }
    }
}
