using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Documents.Client;

namespace NCS.DSS.DigitalIdentity.Cosmos.Helper
{
    public static class DocumentDBHelper
    {
        private static Uri _documentCollectionUri;
        private static readonly string DatabaseId = Environment.GetEnvironmentVariable("DatabaseId");
        private static readonly string CollectionId = Environment.GetEnvironmentVariable("CollectionId");

        public static Uri CreateDocumentCollectionUri()
        {
            if (_documentCollectionUri != null)
                return _documentCollectionUri;

            _documentCollectionUri = UriFactory.CreateDocumentCollectionUri(
                DatabaseId,
                CollectionId);

            return _documentCollectionUri;
        }

        public static Uri CreateDocumentUri(Guid outcomeId)
        {
            return UriFactory.CreateDocumentUri(DatabaseId, CollectionId, outcomeId.ToString());
        }
    }
}
