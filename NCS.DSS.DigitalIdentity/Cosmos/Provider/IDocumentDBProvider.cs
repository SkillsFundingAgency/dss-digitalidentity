using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.Cosmos.Provider
{
    public interface IDocumentDBProvider
    {
        //Task<bool> DoesCustomerResourceExist(Guid customerId);
        //bool DoesIdentityResourceExistAndBelongToCustomer(Guid identityId, Guid customerId);
        Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId);
        Task<Models.DigitalIdentity> GetIdentityAsync(Guid identityId);
        Task<bool> DeleteIdentityAsync(Guid identityId);
        //Task<ResourceResponse<Document>> CreateIdentityAsync(Models.DigitalIdentity action);
        //Task<ResourceResponse<Document>> UpdateIdentityAsync(string action, Guid actionId);
    }
}
