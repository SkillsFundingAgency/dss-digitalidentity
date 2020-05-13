using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentityByCustomerIdHttpTrigger.Service
{
    public class DeleteDigitalIdentityByCustomerIdHttpTriggerService : IDeleteDigitalIdentityByCustomerIdHttpTriggerService
    {
        private readonly IDocumentDBProvider _documentDbProvider;

        public DeleteDigitalIdentityByCustomerIdHttpTriggerService(IDocumentDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public async Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId)
        {
            var identity = await _documentDbProvider.GetIdentityForCustomerAsync(customerId);

            return identity;
        }

        public async Task<bool> DeleteIdentityAsync(Guid identityId)
        {
            var identities = await _documentDbProvider.DeleteIdentityAsync(identityId);

            return identities;
        }
    }
}
