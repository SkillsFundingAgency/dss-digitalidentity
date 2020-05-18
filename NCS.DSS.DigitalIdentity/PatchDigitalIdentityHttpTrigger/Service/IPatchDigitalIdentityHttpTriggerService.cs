using System;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.PatchDigitalIdentityHttpTrigger.Service
{
    public interface IPatchDigitalIdentityHttpTriggerService
    {
        Task SendToServiceBusQueueAsync(Models.DigitalIdentity updatedDigitalIdentity, string reqUrl);

        Task<Models.DigitalIdentity> UpdateIdentity(Models.DigitalIdentity identityResource, Models.DigitalIdentityPatch identityRequestPatch);

        Task<bool> DoesCustomerExists(Guid? identityRequestCustomerId);
    }
}