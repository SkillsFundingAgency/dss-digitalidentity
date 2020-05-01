using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using NCS.DSS.DigitalIdentity.Cosmos.Client;
using NCS.DSS.DigitalIdentity.Cosmos.Helper;
using Newtonsoft.Json.Linq;

namespace NCS.DSS.DigitalIdentity.Cosmos.Provider
{
    public class DocumentDBProvider : IDocumentDBProvider
    {
        public async Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri();

            var client = DocumentDBClient.CreateDocumentClient();

            var identityForCustomerQuery = client
                ?.CreateDocumentQuery<Models.DigitalIdentity>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.CustomerId == customerId)
                .AsDocumentQuery();

            if (identityForCustomerQuery == null)
                return null;

            var digitalIdentity = await identityForCustomerQuery.ExecuteNextAsync<Models.DigitalIdentity>();

            return digitalIdentity?.FirstOrDefault();
        }
    }
}
