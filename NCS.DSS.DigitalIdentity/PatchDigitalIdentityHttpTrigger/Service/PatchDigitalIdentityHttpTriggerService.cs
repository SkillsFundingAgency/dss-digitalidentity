using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.ServiceBus;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DFC.Common.Standard.ServiceBusCleint;
using Newtonsoft.Json.Linq;

namespace NCS.DSS.DigitalIdentity.PatchDigitalIdentityHttpTrigger.Service
{
    public class PatchDigitalIdentityHttpTriggerService : IPatchDigitalIdentityHttpTriggerService
    {
        private readonly IDocumentDBProvider _documentDbProvider;
        private readonly IDigitalIdentityServiceBusClient _serviceBusClient;

        public PatchDigitalIdentityHttpTriggerService(IDocumentDBProvider documentDbProvider, IDigitalIdentityServiceBusClient serviceBusClient)
        {
            _documentDbProvider = documentDbProvider;
            _serviceBusClient = serviceBusClient;
        }

        public async Task SendToServiceBusQueueAsync(Models.DigitalIdentity updatedDigitalIdentity, string reqUrl)
        {
            await _serviceBusClient.SendPatchMessageAsync(updatedDigitalIdentity, reqUrl);
        }

        public async Task<Models.DigitalIdentity> UpdateIdentity(Models.DigitalIdentity identityResource, Models.DigitalIdentityPatch identityRequestPatch)
        {
            if (identityResource == null)
                return null;

            identityRequestPatch.SetDefaultValues();
            identityResource.Patch(identityRequestPatch);

            var response = await _documentDbProvider.UpdateIdentityAsync(identityResource);

            return response;
        }
    }
}
