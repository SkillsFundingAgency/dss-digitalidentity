using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using NCS.DSS.DigitalIdentity.Cosmos.Client;
using NCS.DSS.DigitalIdentity.Cosmos.Helper;
using NCS.DSS.DigitalIdentity.Models;
using Newtonsoft.Json.Linq;

namespace NCS.DSS.DigitalIdentity.Cosmos.Provider
{
    public class DocumentDbProvider : IDocumentDBProvider
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

        public async Task<Models.DigitalIdentity> GetIdentityAsync(Guid identityId)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri();

            var client = DocumentDBClient.CreateDocumentClient();

            var identityForCustomerQuery = client
                ?.CreateDocumentQuery<Models.DigitalIdentity>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.IdentityID == identityId)
                .AsDocumentQuery();

            if (identityForCustomerQuery == null)
                return null;

            var digitalIdentity = await identityForCustomerQuery.ExecuteNextAsync<Models.DigitalIdentity>();

            return digitalIdentity?.FirstOrDefault();
        }

        public async Task<bool> DeleteIdentityAsync(Guid identityId)
        {
            var documentUri = DocumentDBHelper.CreateDocumentUri(identityId);

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
                return false;

            var response = await client.DeleteDocumentAsync(documentUri);
            //204 means that our document has been removed successfully
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        public async Task<ResourceResponse<Document>> CreateContactDetailsAsync(Models.DigitalIdentity digitalIdentity)
        public async Task<Models.DigitalIdentity> GetIdentityByIdentityIdAsync(Guid identityGuid)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri();

            var client = DocumentDBClient.CreateDocumentClient();

            var identityForCustomerQuery = client
                ?.CreateDocumentQuery<Models.DigitalIdentity>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.IdentityID == identityGuid)
                .AsDocumentQuery();

            if (identityForCustomerQuery == null)
                return null;

            var digitalIdentity = await identityForCustomerQuery.ExecuteNextAsync<Models.DigitalIdentity>();

            return digitalIdentity?.FirstOrDefault();
        }

        public async Task<ResourceResponse<Document>> CreateIdentityAsync(Models.DigitalIdentity digitalIdentity)
        {

            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri();

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
                return null;

            var response = await client.CreateDocumentAsync(collectionUri, digitalIdentity);

            return response;

        }

        public async Task<ResourceResponse<Document>> UpdateIdentityAsync(Models.DigitalIdentity digitalIdentity)
        {
            var documentUri = DocumentDBHelper.CreateDocumentUri(digitalIdentity.IdentityID.GetValueOrDefault());

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
                return null;

            var response = await client.ReplaceDocumentAsync(documentUri, digitalIdentity);

            return response;
        }

        public async Task<bool> DoesCustomerResourceExist(Guid customerId)
        {
            var documentUri = DocumentDBHelper.CreateCustomerDocumentUri(customerId);

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
                return false;

            try
            {
                var response = await client.ReadDocumentAsync(documentUri);
                if (response.Resource != null)
                    return true;
            }
            catch (DocumentClientException)
            {
                return false;
            }

            return false;
        }
    }
}
