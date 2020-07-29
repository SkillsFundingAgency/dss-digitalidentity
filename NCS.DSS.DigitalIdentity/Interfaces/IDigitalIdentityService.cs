using NCS.DSS.DigitalIdentity.DTO;
using System;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.Interfaces
{
    public interface IDigitalIdentityService
    {
        Task<Models.DigitalIdentity> CreateAsync(Models.DigitalIdentity digitalIdentity);
        Task<bool> DoesCustomerExists(Guid? identityRequestCustomerId);
        Task<Models.DigitalIdentity> PatchAsync(Models.DigitalIdentity identityResource, DigitalIdentityPatch identityRequestPatch);
        Task<Models.DigitalIdentity> UpdateASync(Models.DigitalIdentity identityResource);
        Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId);
        Task<bool> DeleteIdentityAsync(Guid identityId);
    }
}
