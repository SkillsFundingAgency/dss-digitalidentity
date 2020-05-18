using System;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.GetDigitalIdentityByCustomerIdHttpTrigger.Service
{
    public interface IGetDigitalIdentityByCustomerIdHttpTriggerService
    {
        Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId);

        Task<Models.DigitalIdentity> GetIdentityByIdentityIdAsync(Guid identityGuid);

        Task<bool> DoesCustomerExists(Guid? identityRequestCustomerId);
    }
}