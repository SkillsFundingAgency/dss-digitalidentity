using NCS.DSS.DigitalIdentity.Models;

namespace NCS.DSS.DigitalIdentity.Cosmos.Provider
{
    public interface IDocumentDBProvider
    {
        Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId);
        Task<bool> DeleteIdentityAsync(Guid identityId);

        Task<Models.DigitalIdentity> CreateIdentityAsync(Models.DigitalIdentity digitalIdentity);

        Task<Models.DigitalIdentity> UpdateIdentityAsync(Models.DigitalIdentity digitalIdentity);

        Task<bool> DoesCustomerResourceExist(Guid customerId);
        Task<Models.DigitalIdentity> GetIdentityByIdentityIdAsync(Guid identityGuid);
        Task<Contact> GetCustomerContact(Guid customerId);
        Task<Customer> GetCustomer(Guid customerId);
        Task<bool> GetDigitalIdentityForAnEmailAddress(string emailAddressToCheck);
    }
}
