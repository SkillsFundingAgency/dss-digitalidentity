using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentityHttpTrigger.Service
{
    public class DeleteDigitalIdentityHttpTriggerService : IDeleteDigitalIdentityHttpTriggerService
    {
        private readonly IDocumentDBProvider _documentDbProvider;

        public DeleteDigitalIdentityHttpTriggerService(IDocumentDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public async Task<bool> DeleteIdentityAsync(Guid identityId)
        {
            var identities = await _documentDbProvider.DeleteIdentityAsync(identityId);

            return identities;
        }
    }
}
