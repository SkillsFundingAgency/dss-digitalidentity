using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using NCS.DSS.DigitalIdentity.Models;

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

        Task<ResourceResponse<Document>> CreateIdentityAsync(Models.DigitalIdentity digitalIdentity);

        Task<ResourceResponse<Document>> UpdateIdentityAsync(Models.DigitalIdentity digitalIdentity);

        Task<bool> DoesCustomerResourceExist(Guid customerId);
        Task<Models.DigitalIdentity> GetIdentityByIdentityIdAsync(Guid identityGuid);
    }
}
