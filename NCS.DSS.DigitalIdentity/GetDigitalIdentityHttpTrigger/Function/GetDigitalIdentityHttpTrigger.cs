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
    public class GetDigitalIdentityHttpTrigger
    {
        private readonly IGetDigitalIdentityHttpTriggerService _identityGetService;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly ILogger<GetDigitalIdentityHttpTrigger> _logger;

        public GetDigitalIdentityHttpTrigger(
            IGetDigitalIdentityHttpTriggerService identityGetService,
            IHttpRequestHelper httpRequestHelper,
            ILogger<GetDigitalIdentityHttpTrigger> logger)
        {
            _identityGetService = identityGetService;
            _httpRequestHelper = httpRequestHelper;
            _logger = logger;
        }

        [Function("GetByIdentityId")]
        [ProducesResponseType(typeof(Models.DigitalIdentity), (int)HttpStatusCode.OK)]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity found", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Digital Identity does not exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Request was malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Display(Name = "GetByIdentityId", Description = "Ability to retrieve an individual digital identity")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "identities/{identityId}")] HttpRequest req, string identityId)
        {
            _logger.LogInformation($"Function {nameof(GetDigitalIdentityHttpTrigger)} has been invoked");

            var correlationId = _httpRequestHelper.GetDssCorrelationId(req);
            if (string.IsNullOrEmpty(correlationId))
            {
                _logger.LogInformation("Unable to locate 'DssCorrelationId' in request header");
            }

            if (!Guid.TryParse(correlationId, out var correlationGuid))
            {
                _logger.LogInformation("Unable to parse 'DssCorrelationId' to a GUID");
                correlationGuid = Guid.NewGuid();
            }

            var touchpointId = _httpRequestHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                _logger.LogInformation($"Unable to locate 'TouchpointId' in request header. Correlation GUID: {correlationGuid}");
                return new BadRequestResult();
            }

            if (!Guid.TryParse(identityId, out var identityGuid))
            {
                _logger.LogInformation($"Unable to parse 'identityId' to a GUID. Digital Identity ID: {identityId}");
                return new BadRequestObjectResult(identityGuid.ToString());
            }

            _logger.LogInformation($"Header validation has succeeded. Touchpoint ID: {touchpointId}. Correlation GUID: {correlationGuid}");
            _logger.LogInformation($"Attempting to retrieve DIGITAL IDENTITY ID. Digital Identity GUID: {identityGuid}");

            var identity = await _identityGetService.GetIdentityAsync(identityGuid);

            if (identity != null)
            {
                _logger.LogInformation($"DIGITAL IDENTITY successfully retrieved. Digital Identity ID: {identity.IdentityID.Value}");
                _logger.LogInformation($"Function {nameof(GetDigitalIdentityHttpTrigger)} has finished invoking");

                return new JsonResult(identity, new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            else
            {
                _logger.LogInformation($"DIGITAL IDENTITY does not exist. Digital Identity ID: {identityId}");
                _logger.LogInformation($"Function {nameof(GetDigitalIdentityHttpTrigger)} has finished invoking");

                return new NoContentResult();
            }
        }
    }
}
