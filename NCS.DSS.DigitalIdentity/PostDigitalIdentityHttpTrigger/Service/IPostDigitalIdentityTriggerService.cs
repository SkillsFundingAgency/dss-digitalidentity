using System;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Service
{
    public interface IPostDigitalIdentityTriggerService
    {
        Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId);

        Task<Models.DigitalIdentity> CreateAsync(Models.DigitalIdentity digitalIdentity);
        Task SendToServiceBusQueueAsync(Models.DigitalIdentity digitalIdentity, string apimUrl);
    }
}