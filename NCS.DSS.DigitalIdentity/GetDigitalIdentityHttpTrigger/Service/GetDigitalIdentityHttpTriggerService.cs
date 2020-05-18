using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service
{
    public class GetDigitalIdentityHttpTriggerService : IGetDigitalIdentityHttpTriggerService
    {
        private readonly IDocumentDBProvider _documentDbProvider;

        public GetDigitalIdentityHttpTriggerService(IDocumentDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public async Task<Models.DigitalIdentity> GetIdentityAsync(Guid identityId)
        {
            var identity = await _documentDbProvider.GetIdentityByIdentityIdAsync(identityId);

            return identity;
        }
    }
}
