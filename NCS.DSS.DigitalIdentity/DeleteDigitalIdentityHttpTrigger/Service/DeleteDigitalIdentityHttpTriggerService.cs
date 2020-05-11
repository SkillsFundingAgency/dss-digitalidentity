﻿using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentityHttpTrigger.Service
{
    public class DeleteDigitalIdentityByCustomerIdHttpTriggerService : IDeleteDigitalIdentityByCustomerIdHttpTriggerService
    {
        private readonly IDocumentDBProvider _documentDbProvider;

        public DeleteDigitalIdentityByCustomerIdHttpTriggerService(IDocumentDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public async Task<bool> DeleteIdentityAsync(Guid identityId)
        {
            var documentDbProvider = new DocumentDBProvider();
            var identities = await documentDbProvider.DeleteIdentityAsync(identityId);

            return identities;
        }
    }
}
