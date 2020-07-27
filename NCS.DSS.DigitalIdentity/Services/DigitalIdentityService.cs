﻿using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.Interfaces;
using System;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.Services
{
    public class DigitalIdentityService : IDigitalIdentityService
    {
        private readonly IDocumentDBProvider _documentDbProvider;
        private readonly IDigitalIdentityServiceBusClient _serviceBusClient;

        public DigitalIdentityService(IDocumentDBProvider documentDbProvider, IDigitalIdentityServiceBusClient serviceBusClient)
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

        public async Task<bool> DoesCustomerExists(Guid? identityRequestCustomerId)
        {
            return identityRequestCustomerId != null && await _documentDbProvider.DoesCustomerResourceExist(identityRequestCustomerId.Value);
        }

        public async Task<Models.DigitalIdentity> PatchAsync(Models.DigitalIdentity identityResource, Models.DigitalIdentityPatch identityRequestPatch)
        {
            if (identityResource == null)
                return null;

            identityRequestPatch.SetDefaultValues();
            identityResource.Patch(identityRequestPatch);

            var response = await UpdateASync(identityResource);

            return response;
        }

        public async Task<Models.DigitalIdentity> UpdateASync(Models.DigitalIdentity identityResource)
        {
            if (identityResource == null)
                return null;
            var response = await _documentDbProvider.UpdateIdentityAsync(identityResource);

            return response;
        }
    }
}