using System;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service
{
    public interface IGetDigitalIdentityHttpTriggerService
    {
        Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId);

        Task<Models.DigitalIdentity> GetIdentityByIdentityIdAsync(Guid identityGuid);

        Task<bool> DoesCustomerExists(Guid? identityRequestCustomerId);
        Task<Models.DigitalIdentity> GetIdentityAsync(Guid customerId);
    }
}
