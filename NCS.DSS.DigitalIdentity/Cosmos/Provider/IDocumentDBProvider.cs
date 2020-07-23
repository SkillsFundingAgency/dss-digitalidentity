using NCS.DSS.DigitalIdentity.Models;
using System;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.Cosmos.Provider
{
    public interface IDocumentDBProvider
    {
        //Task<bool> DoesCustomerResourceExist(Guid customerId);
        //bool DoesIdentityResourceExistAndBelongToCustomer(Guid identityId, Guid customerId);
        Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId);
        Task<bool> DeleteIdentityAsync(Guid identityId);
        //Task<ResourceResponse<Document>> CreateIdentityAsync(Models.DigitalIdentity action);
        //Task<ResourceResponse<Document>> UpdateIdentityAsync(string action, Guid actionId);

        Task<Models.DigitalIdentity> CreateIdentityAsync(Models.DigitalIdentity digitalIdentity);

        Task<Models.DigitalIdentity> UpdateIdentityAsync(Models.DigitalIdentity digitalIdentity);

        Task<bool> DoesCustomerResourceExist(Guid customerId);
        Task<Models.DigitalIdentity> GetIdentityByIdentityIdAsync(Guid identityGuid);
        Task<Contact> GetCustomerContact(Guid customerId);
        Task<Customer> GetCustomer(Guid customerId);
        Task<bool> DoesContactDetailsWithEmailExists(string emailAddressToCheck);
    }
}
