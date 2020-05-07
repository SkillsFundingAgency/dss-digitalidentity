using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using System;
using System.Collections.Generic;
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
    }
}
