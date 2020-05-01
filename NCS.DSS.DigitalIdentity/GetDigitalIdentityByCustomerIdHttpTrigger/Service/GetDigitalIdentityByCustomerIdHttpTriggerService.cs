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
            var actionPlan = await _documentDbProvider.GetIdentityForCustomerAsync(customerId);

            return actionPlan;
        }
    }
}
