using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.GetDigitalIdentityByCustomerIdHttpTrigger.Service
{
    public class GetDigitalIdentityByCustomerIdHttpTriggerService : IGetDigitalIdentityByCustomerIdHttpTriggerService
    {
        private readonly IDocumentDBProvider _documentDbProvider;

        public GetDigitalIdentityByCustomerIdHttpTriggerService(IDocumentDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public async Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId)
        {
            var identity = await _documentDbProvider.GetIdentityForCustomerAsync(customerId);

            return identity;
        }

        public async Task<Models.DigitalIdentity> GetIdentityByIdentityIdAsync(Guid identityGuid)
        {
            var identity = await _documentDbProvider.GetIdentityByIdentityIdAsync(identityGuid);

            return identity;
        }

        public async Task<bool> DoesCustomerExists(Guid? identityRequestCustomerId)
        {
            return identityRequestCustomerId != null && await _documentDbProvider.DoesCustomerResourceExist(identityRequestCustomerId.Value);
        }
    }
}
