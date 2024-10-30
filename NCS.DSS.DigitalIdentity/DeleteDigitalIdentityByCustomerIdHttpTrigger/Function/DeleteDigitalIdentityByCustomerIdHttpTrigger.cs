using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.DigitalIdentity.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentityByCustomerIdHttpTrigger.Function
{
    public class DeleteDigitalIdentityByCustomerIdHttpTrigger
    {
        private readonly IDigitalIdentityService _identityDeleteService;
        private readonly IDigitalIdentityServiceBusClient _serviceBus;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly ILogger<DeleteDigitalIdentityByCustomerIdHttpTrigger> _logger;

        public DeleteDigitalIdentityByCustomerIdHttpTrigger(
            IDigitalIdentityService deleteService,
            IDigitalIdentityServiceBusClient serviceBus,
            IHttpRequestHelper httpRequestHelper,
            ILogger<DeleteDigitalIdentityByCustomerIdHttpTrigger> logger)
        {
            _identityDeleteService = deleteService;
            _serviceBus = serviceBus;
            _httpRequestHelper = httpRequestHelper;
            _logger = logger;
        }

        [Function("DeleteByCustomerId")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity found", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Digital Identity does not exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Request was malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Display(Name = "DeleteByCustomerId", Description = "Ability to delete an individual digital identity by customer id")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customer/{customerId}")] HttpRequest req, string customerId)
        {
            _logger.LogInformation($"Function {nameof(DeleteDigitalIdentityByCustomerIdHttpTrigger)} has been invoked");

            var apimUrl = _httpRequestHelper.GetDssApimUrl(req);
            var touchpointId = _httpRequestHelper.GetDssTouchpointId(req);
            var correlationId = _httpRequestHelper.GetDssCorrelationId(req);

            if (string.IsNullOrEmpty(correlationId))
            {
                _logger.LogInformation("Unable to locate 'DssCorrelationId' in request header");
            }

            if (!Guid.TryParse(correlationId, out var correlationGuid))
            {
                _logger.LogInformation("Unable to parse 'DssCorrelationId' to a Guid");
                correlationGuid = Guid.NewGuid();
            }

            if (string.IsNullOrEmpty(touchpointId))
            {
                _logger.LogInformation($"Unable to locate 'TouchpointId' in request header. Correlation GUID: {correlationGuid}");
                return new BadRequestResult();
            }

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                _logger.LogInformation($"Unable to parse 'customerId' to a GUID. Customer ID: {customerId}. Correlation GUID: {correlationGuid}");
                return new BadRequestObjectResult(customerGuid.ToString());
            }

            _logger.LogInformation($"Header validation has succeeded. Touchpoint ID: {touchpointId}. Correlation GUID: {correlationGuid}");

            var customerExists = await _identityDeleteService.DoesCustomerExists(customerGuid);
            
            if (!customerExists)
            {
                _logger.LogInformation($"Customer does not exist. Customer GUID: {customerGuid}");
                return new BadRequestResult();
            }

            // First get the identity from the customer id
            _logger.LogInformation($"Attempting to retrieve DIGITAL IDENTITY for Customer. Customer GUID: {customerGuid}. Correlation GUID: {correlationGuid}");
            var identity = await _identityDeleteService.GetIdentityForCustomerAsync(customerGuid);

            if (identity == null)
            {
                _logger.LogInformation($"DIGITAL IDENTITY does not exist for Customer. Customer GUID: {customerGuid}. Customer ID: {customerId}");
                return new NoContentResult();
            }

            _logger.LogInformation($"Attempting to update DIGITAL IDENTITY. Digital Identity ID: {identity?.IdentityID.Value}");

            // Trigger change feed
            identity.DateOfClosure = DateTime.Now;
            identity.LastModifiedTouchpointId = touchpointId;
            identity.ttl = 10; //stored in seconds
            var deleteRequest = await _identityDeleteService.UpdateASync(identity);

            if (deleteRequest != null)
            {
                _logger.LogInformation($"Successfully updated DIGITAL IDENTITY. Digital Identity ID: {identity?.IdentityID.Value}");
            } 
            else
            {
                _logger.LogInformation($"Failed to updated DIGITAL IDENTITY. Digital Identity ID: {identity?.IdentityID.Value}"); // should this be an ERROR?
            }

            _logger.LogInformation($"Setting DIGITAL IDENTITY as deleted. Digital Identity ID: {identity?.IdentityID.Value}");
            identity.SetDeleted();

            _logger.LogInformation($"Attempting to send deletion notification to Service Bus Namespace");
            await _serviceBus.SendDeleteMessageAsync(identity, apimUrl); // convert this to return a success/failure status. Then add corresponding success and failure logs

            _logger.LogInformation($"Function {nameof(DeleteDigitalIdentityByCustomerIdHttpTrigger)} has finished invoking");

            return new OkResult();
        }
    }
}
