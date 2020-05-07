using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Service
{
    public class PostDigitalIdentityHttpTriggerService : IPostDigitalIdentityTriggerService
    {
        private readonly IDocumentDBProvider _documentDbProvider;

        public PostDigitalIdentityHttpTriggerService(IDocumentDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public async Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId)
        {
            var identity = await _documentDbProvider.GetIdentityForCustomerAsync(customerId);

            return identity;
        }

        public async Task<Models.DigitalIdentity> CreateAsync(Models.DigitalIdentity digitalIdentity)
        {
            if (digitalIdentity == null)
                return null;

            digitalIdentity.SetDefaultValues();

            var documentDbProvider = new DocumentDBProvider();

            var response = await documentDbProvider.CreateContactDetailsAsync(digitalIdentity);

            return response.StatusCode == HttpStatusCode.Created ? (dynamic)response.Resource : (Guid?)null;
        }
    }
}
