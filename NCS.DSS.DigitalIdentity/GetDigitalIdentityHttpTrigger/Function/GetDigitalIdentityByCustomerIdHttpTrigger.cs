using DFC.Common.Standard.Logging;
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
        private readonly ILoggerHelper _loggerHelper;
        private readonly ILogger _logger;

        public GetDigitalIdentityByCustomerIdHttpTrigger(
            IGetDigitalIdentityHttpTriggerService identityGetService,
            IHttpRequestHelper httpRequestHelper,
            ILoggerHelper loggerHelper,
            ILogger<GetDigitalIdentityByCustomerIdHttpTrigger> logger)
        {
            _identityGetService = identityGetService;
            _httpRequestHelper = httpRequestHelper;
            _loggerHelper = loggerHelper;
            _logger = logger;
        }

        [Function("GetById")]
        [ProducesResponseType(typeof(Models.DigitalIdentity), (int)HttpStatusCode.OK)]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity found", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Digital Identity does not exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Request was malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Display(Name = "GetById", Description = "Ability to retrieve an individual digital identity for the given customer")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{customerId}")] HttpRequest req, string customerId)
        {
            _loggerHelper.LogMethodEnter(_logger);

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
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, "Unable to locate 'TouchpointId' in request header");
                return new BadRequestResult();
            }

            _loggerHelper.LogInformationMessage(_logger, correlationGuid,
                string.Format("Get Digital Identity By Id C# HTTP trigger function  processed a request. By Touchpoint: {0}",
                    touchpointId));

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Unable to parse 'customerId' to a Guid: {0}", customerId));
                return new BadRequestObjectResult(customerGuid.ToString());
            }

            _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Attempting to see if customer exists {0}", customerGuid));
            var doesCustomerExist = await _identityGetService.DoesCustomerExists(customerGuid);

            if (!doesCustomerExist)
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Customer does not exist {0}", customerGuid));
                return new NoContentResult();
            }

            _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Attempting to get identity for customer {0}", customerGuid));
            var identity = await _identityGetService.GetIdentityForCustomerAsync(customerGuid);

            _loggerHelper.LogMethodExit(_logger);

            if (identity == null)
            {
                return new NoContentResult();

            }

            return new JsonResult(identity, new JsonSerializerOptions())
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }
    }
}
