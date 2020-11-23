using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NCS.DSS.DigitalIdentity.Cosmos.Helper;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.Interfaces;
using System;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentitiesNotActivatedTrigger.Function
{
    public class DeleteDigitalIdentitiesNoActivatedTrigger
    {
        private readonly IDigitalIdentityService _identityDeleteService;
        private readonly ILogger<DeleteDigitalIdentitiesNoActivatedTrigger> _logger;
        private readonly IDocumentDBProvider _resourceHelper;
        public DeleteDigitalIdentitiesNoActivatedTrigger(IDigitalIdentityService deleteService, ILogger<DeleteDigitalIdentitiesNoActivatedTrigger> logger, IDocumentDBProvider resourceHelper)
        {
            _identityDeleteService = deleteService;
            _logger = logger;
            _resourceHelper = resourceHelper;
        }


        [FunctionName("DeleteDigitalIdentitiesNoActivatedTrigger")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            var unactivated = _resourceHelper.GetUnactivatedAccounts();
            foreach (var di in unactivated)
            {
                try
                {
                    di.DateOfClosure = DateTime.Now;
                    //       di.LastModifiedTouchpointId = "Automatically deleted by DeleteDigitalIdentitiesNoActivatedTrigger";
                    //       await _identityDeleteService.UpdateASync(identity);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error occurred when attempted to delete unactivated DigitalIdentity: {di.IdentityID} ");
                }

                _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            }
        }
    }
}
