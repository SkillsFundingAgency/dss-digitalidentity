using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentitiesNotActivatedTrigger.Function
{
    public class DeleteDigitalIdentitiesNoActivatedTrigger
    {
        private readonly IDocumentDBProvider _resourceHelper;
        public DeleteDigitalIdentitiesNoActivatedTrigger( IDocumentDBProvider resourceHelper)
        {
            _resourceHelper = resourceHelper;
        }

        [FunctionName("DeleteDigitalIdentitiesNoActivatedTrigger")]
        public async Task Run([TimerTrigger("%DeleteDigitalIdentitiesNoActivatedTrigger%")] TimerInfo myTimer, ILogger log)
        {
            var unactivated = _resourceHelper.GetUnactivatedAccounts();
            foreach (var di in unactivated)
            {
                try
                {
                    di.DateOfClosure = DateTime.Now;
                    di.ttl = 10;
                    //di.LastModifiedTouchpointId = "Automatically deleted by DeleteDigitalIdentitiesNoActivatedTrigger";
                    //await _identityDeleteService.UpdateASync(di);
                }
                catch (Exception e)
                {
                    log.LogError($"Error occurred when attempted to delete unactivated DigitalIdentity: {di.IdentityID} - {e.InnerException}");
                }

                log.LogInformation($"Automatically removed {unactivated?.Count()}");
            }

            log.LogInformation($"Automatically removed {unactivated?.Count()} old Digital Identities");
        }
    }
}