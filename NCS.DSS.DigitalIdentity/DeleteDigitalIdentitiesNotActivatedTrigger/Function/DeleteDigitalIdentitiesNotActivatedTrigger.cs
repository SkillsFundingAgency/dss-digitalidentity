using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentitiesNotActivatedTrigger.Function
{
    public class DeleteDigitalIdentitiesNotActivatedTrigger
    {
        private readonly IDocumentDBProvider _resourceHelper;
        private readonly ILogger<DeleteDigitalIdentitiesNotActivatedTrigger> _logger;
        private readonly IDigitalIdentityService _identityDeleteService;
        public DeleteDigitalIdentitiesNotActivatedTrigger(IDigitalIdentityService deleteService, IDocumentDBProvider resourceHelper, ILogger<DeleteDigitalIdentitiesNotActivatedTrigger> log)
        {
            _resourceHelper = resourceHelper;
            _logger = log;
            _identityDeleteService = deleteService;
        }

        [FunctionName("DeleteDigitalIdentitiesNoActivatedTrigger")]
        public async Task Run([TimerTrigger("%DeleteDigitalIdentitiesNotActivatedTrigger%", RunOnStartup = true)] TimerInfo myTimer)
        {
            var unactivated = _resourceHelper.GetUnactivatedAccounts();
            foreach (var di in unactivated)
            {
                try
                {
                    di.DateOfClosure = DateTime.Now;
                    di.ttl = 10;
                    di.LastModifiedTouchpointId = "Automatically deleted by DeleteDigitalIdentitiesNoActivatedTrigger";
                    await _identityDeleteService.UpdateASync(di);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error occurred when attempted to delete unactivated DigitalIdentity: {di.IdentityID} - {e.InnerException}");
                }
            }
            _logger.LogInformation($"Automatically removed {unactivated?.Count()} old Digital Identities");
        }
    }
}