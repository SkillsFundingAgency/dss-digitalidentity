using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using NCS.DSS.DigitalIdentity.Cosmos.Client;
using NCS.DSS.DigitalIdentity.Cosmos.Helper;
using NCS.DSS.DigitalIdentity.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

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

        public async Task<Models.DigitalIdentity> CreateIdentityAsync(Models.DigitalIdentity digitalIdentity)
        {

            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri();

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
                return null;

            var response = await client.CreateDocumentAsync(collectionUri, digitalIdentity);

            return response.StatusCode == HttpStatusCode.Created ? (dynamic)response.Resource : null;

        }

        public async Task<Models.DigitalIdentity> UpdateIdentityAsync(Models.DigitalIdentity digitalIdentity)
        {
            var documentUri = DocumentDBHelper.CreateDocumentUri(digitalIdentity.IdentityID.GetValueOrDefault());

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
                return null;

            var response = await client.ReplaceDocumentAsync(documentUri, digitalIdentity);

            return response.StatusCode == HttpStatusCode.OK ? (dynamic)response.Resource : null;

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

        public async Task<Contact> GetCustomerContact(Guid customerId)
        {
            var collectionUri = DocumentDBHelper.CreateContactUri();

            var client = DocumentDBClient.CreateDocumentClient();

            var query = client
                ?.CreateDocumentQuery<Contact>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.CustomerId == customerId)
                .AsDocumentQuery();

            if (query == null)
                return null;

            var contact = await query.ExecuteNextAsync<Contact>();

            return contact?.FirstOrDefault();
        }


        public async Task<Customer> GetCustomer(Guid customerId)
        {
            var documentUri = DocumentDBHelper.CreateCustomerDocumentUri(customerId);
            var client = DocumentDBClient.CreateDocumentClient();
            if (client == null)
                return null;
            try
            {
                var response = await client.ReadDocumentAsync<Customer>(documentUri);
                if (response != null)
                    return response;
            }
            catch (DocumentClientException)
            {
                return null;
            }

            return null;
        }

        public async Task<bool> DoesContactDetailsWithEmailExists(string emailAddressToCheck)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri();
            var client = DocumentDBClient.CreateDocumentClient();
            var contactDetailsForEmailQuery = client
                ?.CreateDocumentQuery<Contact>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.EmailAddress == emailAddressToCheck)
                .AsDocumentQuery();
            if (contactDetailsForEmailQuery == null)
                return false;

            var contactDetails = await contactDetailsForEmailQuery.ExecuteNextAsync<Contact>();
            return contactDetails.Any();
        }
    }
}
