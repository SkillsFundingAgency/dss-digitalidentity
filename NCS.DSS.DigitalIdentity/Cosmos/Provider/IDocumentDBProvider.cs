﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace NCS.DSS.DigitalIdentity.Cosmos.Provider
{
    public interface IDocumentDBProvider
    {
        //Task<bool> DoesCustomerResourceExist(Guid customerId);
        //bool DoesIdentityResourceExistAndBelongToCustomer(Guid identityId, Guid customerId);
        Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId);
        //Task<ResourceResponse<Document>> CreateIdentityAsync(Models.DigitalIdentity action);
        //Task<ResourceResponse<Document>> UpdateIdentityAsync(string action, Guid actionId);

        Task<ResourceResponse<Document>> CreateContactDetailsAsync(Models.DigitalIdentity digitalIdentity);
    }
}
