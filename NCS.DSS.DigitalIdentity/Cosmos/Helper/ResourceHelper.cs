using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;

namespace NCS.DSS.DigitalIdentity.Cosmos.Helper
{
    public class ResourceHelper : IResourceHelper
    {
        public async Task<bool> DoesCustomerExist(Guid customerId)
        {
            var documentDbProvider = new DocumentDBProvider();
            var doesCustomerExist = await documentDbProvider.DoesCustomerResourceExist(customerId);

            return doesCustomerExist;
        }

    }
}
