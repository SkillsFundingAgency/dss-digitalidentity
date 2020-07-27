using System;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.Interfaces
{
    public interface IDigitalIdentityService
    {
        Task<Models.DigitalIdentity> CreateAsync(Models.DigitalIdentity digitalIdentity);
        Task<bool> DoesCustomerExists(Guid? identityRequestCustomerId);
        Task<Models.DigitalIdentity> PatchAsync(Models.DigitalIdentity identityResource, Models.DigitalIdentityPatch identityRequestPatch);
        Task<Models.DigitalIdentity> UpdateASync(Models.DigitalIdentity identityResource);
    }
}
