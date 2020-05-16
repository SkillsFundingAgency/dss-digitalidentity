using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.ServiceBus;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Service
{
    public class PostDigitalIdentityHttpTriggerService : IPostDigitalIdentityHttpTriggerService
    {
        private readonly IDocumentDBProvider _documentDbProvider;
        private readonly IDigitalIdentityServiceBusClient _serviceBusClient;

        public PostDigitalIdentityHttpTriggerService(IDocumentDBProvider documentDbProvider, IDigitalIdentityServiceBusClient serviceBusClient)
        {
            _documentDbProvider = documentDbProvider;
            _serviceBusClient = serviceBusClient;
        }

        public async Task<Models.DigitalIdentity> CreateAsync(Models.DigitalIdentity digitalIdentity)
        {
            if (digitalIdentity == null)
                return null;

            digitalIdentity.SetDefaultValues();

            var response = await _documentDbProvider.CreateIdentityAsync(digitalIdentity);

            return response;
        }

        public async Task SendToServiceBusQueueAsync(Models.DigitalIdentity digitalIdentity, string reqUrl)
        {
            await _serviceBusClient.SendPostMessageAsync(digitalIdentity, reqUrl);
        }
    }
}
