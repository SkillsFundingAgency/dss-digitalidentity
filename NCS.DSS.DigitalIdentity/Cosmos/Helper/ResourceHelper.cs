using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;

namespace NCS.DSS.DigitalIdentity.Cosmos.Helper
{
    public class ResourceHelper : IResourceHelper
    {
        private readonly IDocumentDBProvider _documentDbProvider;

        public ResourceHelper(IDocumentDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public async Task<bool> DoesCustomerExist(Guid customerId)
        {
            var doesCustomerExist = await _documentDbProvider.DoesCustomerResourceExist(customerId);

            return doesCustomerExist;
        }

    }
}
