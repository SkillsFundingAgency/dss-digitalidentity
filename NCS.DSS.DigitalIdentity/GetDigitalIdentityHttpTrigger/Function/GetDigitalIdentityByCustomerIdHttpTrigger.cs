using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Function
{
    public class GetDigitalIdentityByCustomerIdHttpTrigger
    {
        private readonly IGetDigitalIdentityHttpTriggerService _identityGetService;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly ILogger<GetDigitalIdentityByCustomerIdHttpTrigger> _logger;

        public GetDigitalIdentityByCustomerIdHttpTrigger(
            IGetDigitalIdentityHttpTriggerService identityGetService,
            IHttpRequestHelper httpRequestHelper,
            ILogger<GetDigitalIdentityByCustomerIdHttpTrigger> logger)
        {
            _identityGetService = identityGetService;
            _httpRequestHelper = httpRequestHelper;
            _logger = logger;
        }

        [Function("GetByCustomerId")]
        [ProducesResponseType(typeof(Models.DigitalIdentity), (int)HttpStatusCode.OK)]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity found", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Digital Identity does not exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Request was malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Display(Name = "GetByCustomerId", Description = "Ability to retrieve an individual digital identity for the given customer")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{customerId}")] HttpRequest req, string customerId)
        {
            _logger.LogInformation($"Function {nameof(GetDigitalIdentityByCustomerIdHttpTrigger)} has been invoked");

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

            var touchpointId = _httpRequestHelper.GetDssTouchpointId(req);
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
            _logger.LogInformation($"Attempting to see if customer exists. Customer GUID: {customerGuid}");

            var doesCustomerExist = await _identityGetService.DoesCustomerExists(customerGuid);

            if (!doesCustomerExist)
            {
                _logger.LogInformation($"Customer does not exist. Customer GUID: {customerGuid}");
                return new NoContentResult();
            }
            else
            {
                _logger.LogInformation($"Customer does exist. Customer GUID: {customerGuid}");
            }

            _logger.LogInformation($"Attempting to retrieve DIGITAL IDENTITY for Customer. Customer GUID: {customerGuid}");
            var identity = await _identityGetService.GetIdentityForCustomerAsync(customerGuid);

            if (identity != null)
            {
                _logger.LogInformation($"DIGITAL IDENTITY successfully retrieved. Digital Identity ID: {identity.IdentityID.Value}");
                _logger.LogInformation($"Function {nameof(GetDigitalIdentityByCustomerIdHttpTrigger)} has finished invoking");
                
                return new JsonResult(identity, new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            else
            {
                _logger.LogInformation($"DIGITAL IDENTITY does not exist for Customer. Customer GUID: {customerGuid}");
                _logger.LogInformation($"Function {nameof(GetDigitalIdentityByCustomerIdHttpTrigger)} has finished invoking");
                
                return new NoContentResult();
            }
        }
    }
}
