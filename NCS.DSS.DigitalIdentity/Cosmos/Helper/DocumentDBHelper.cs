using Microsoft.Azure.Documents.Client;

namespace NCS.DSS.DigitalIdentity.Cosmos.Helper
{
    public static class DocumentDBHelper
    {
        private static Uri _documentCollectionUri;
        private static readonly string DatabaseId = Environment.GetEnvironmentVariable("DatabaseId");
        private static readonly string CollectionId = Environment.GetEnvironmentVariable("CollectionId");
        private static readonly string CustomerDatabaseId = Environment.GetEnvironmentVariable("CustomerDatabaseId");
        private static readonly string CustomerCollectionId = Environment.GetEnvironmentVariable("CustomerCollectionId");
        private static readonly string ContactsCollectionId = Environment.GetEnvironmentVariable("ContactsCollectionId");
        private static readonly string ContactsDatabaseId = Environment.GetEnvironmentVariable("ContactsDatabaseId");

        public static Uri CreateDocumentCollectionUri()
        {
            if (_documentCollectionUri != null)
                return _documentCollectionUri;

            _documentCollectionUri = UriFactory.CreateDocumentCollectionUri(
                DatabaseId,
                CollectionId);

            return _documentCollectionUri;
        }

        public static Uri CreateContactUri()
        {
            return UriFactory.CreateDocumentCollectionUri(ContactsDatabaseId, ContactsCollectionId);
        }

        public static Uri CreateDocumentUri(Guid outcomeId)
        {
            return UriFactory.CreateDocumentUri(DatabaseId, CollectionId, outcomeId.ToString());
        }

        public static Uri CreateCustomerDocumentUri(Guid customerId)
        {
            return UriFactory.CreateDocumentUri(CustomerDatabaseId, CustomerCollectionId, customerId.ToString());
        }
    }
}
