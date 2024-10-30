using AutoMapper;
using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.DigitalIdentity.Cosmos.Helper;
using NCS.DSS.DigitalIdentity.DTO;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.Interfaces;
using System.Net;
using System.Text.Json;
using System.Web.Http;
using JsonException = Newtonsoft.Json.JsonException;

namespace NCS.DSS.DigitalIdentity.PatchDigitalIdentityHttpTrigger.Function
{
    public class PatchDigitalIdentityByIdentityIdHttpTrigger
    {
        private readonly IDigitalIdentityService _identityPatchService;
        private readonly IGetDigitalIdentityHttpTriggerService _identityGetService;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IValidate _validate;
        private readonly ILogger<PatchDigitalIdentityByIdentityIdHttpTrigger> _logger;
        private readonly IMapper _mapper;
        private readonly IDynamicHelper _dynamicHelper;
        private static readonly string[] PropertyToExclude = { "TargetSite" };

        public PatchDigitalIdentityByIdentityIdHttpTrigger(
            IDigitalIdentityService identityPatchService,
            IGetDigitalIdentityHttpTriggerService identityGetService,
            IHttpRequestHelper httpRequestHelper,
            IValidate validate,
            ILogger<PatchDigitalIdentityByIdentityIdHttpTrigger> logger,
            IMapper mapper,
            IDynamicHelper dynamicHelper)
        {
            _identityPatchService = identityPatchService;
            _identityGetService = identityGetService;
            _httpRequestHelper = httpRequestHelper;
            _validate = validate;
            _logger = logger;
            _mapper = mapper;
            _dynamicHelper = dynamicHelper;
        }

        [Function("PatchByIdentityId")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity Patched", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Patch request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Digital Identity resource validation error(s)", ShowSchema = false)]
        [ProducesResponseType(typeof(Models.DigitalIdentity), (int)HttpStatusCode.OK)]
        [PostRequestBody(typeof(DigitalIdentityPatch), "Digital Identity Request body")]
        //[Display(Name = "PatchByIdentityId", Description = "Lorum ipsum - what do I do?")] --> should I have this?
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "identity/{IdentityId}")] HttpRequest req, string IdentityId)
        {
            _logger.LogInformation($"Function {nameof(PatchDigitalIdentityByIdentityIdHttpTrigger)} has been invoked");

            // Get Correlation Id
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

            // Get Touch point Id
            var touchpointId = _httpRequestHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                _logger.LogInformation($"Unable to locate 'TouchpointId' in request header. Correlation GUID: {correlationGuid}");
                return new BadRequestResult();
            }

            // Get Apim URL
            var apimUrl = _httpRequestHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(apimUrl))
            {
                _logger.LogInformation($"Unable to locate 'apimurl' in request header. Correlation GUID: {correlationGuid}");
                return new BadRequestResult();
            }

            _logger.LogInformation($"APIM URL: {apimUrl}");

            if (!Guid.TryParse(IdentityId, out var identityGuid))
            {
                _logger.LogInformation($"Unable to parse 'IdentityId' to a GUID. Digital Identity ID: {IdentityId}");
                return new BadRequestObjectResult(identityGuid.ToString());
            }

            _logger.LogInformation($"Header validation has succeeded. Touchpoint ID: {touchpointId}. Correlation GUID: {correlationGuid}");
            _logger.LogInformation($"Attempting to get resource from request body. Correlation GUID: {correlationGuid}");

            // Get patch body
            DigitalIdentityPatch digitalPatchRequest;
            try
            {
                digitalPatchRequest = await _httpRequestHelper.GetResourceFromRequest<DigitalIdentityPatch>(req);
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Unable to parse DigitalIdentityPatch from request body. Correlation GUID: {correlationGuid}. Exception: {ex.Message}");
                return new UnprocessableEntityObjectResult(_dynamicHelper.ExcludeProperty(ex, PropertyToExclude));
            }

            if (digitalPatchRequest == null)
            {
                _logger.LogError($"DigitalIdentityPatch object is NULL. Correlation GUID: {correlationGuid}");
                return new UnprocessableEntityResult();
            }

            // Validate patch body
            _logger.LogInformation($"Attempting to validate DigitalIdentityPatch object. Correlation GUID: {correlationGuid}");
            var errors = await _validate.ValidateResource(digitalPatchRequest, false);

            if (errors != null && errors.Any())
            {
                _logger.LogError($"Validation for DigitalIdentityPatch object failed. Correlation GUID: {correlationGuid}");
                return new UnprocessableEntityObjectResult(errors);
            }

            var patchRequestCustomerIdNotEmpty = !digitalPatchRequest.CustomerId.Equals(Guid.Empty);

            // Check if customer exists then validate
            if (patchRequestCustomerIdNotEmpty)
            {
                var customerExists = await _identityPatchService.DoesCustomerExists(digitalPatchRequest.CustomerId);

                if (customerExists)
                {
                    _logger.LogInformation($"Customer exists. Customer ID: {digitalPatchRequest.CustomerId}");
                }
                else
                {
                    _logger.LogError($"Customer does not exist. Customer ID: {digitalPatchRequest.CustomerId}");
                    return new UnprocessableEntityObjectResult($"Customer with CustomerId  {digitalPatchRequest.CustomerId} does not exists.");
                }
            }

            digitalPatchRequest.LastModifiedTouchpointId = touchpointId;

            // Check if resource exists
            _logger.LogInformation($"Attempting to retrieve DIGITAL IDENTITY. Digital Identity GUID: {identityGuid}");
            var digitalIdentity = await _identityGetService.GetIdentityAsync(identityGuid);

            if (digitalIdentity == null)
            {
                _logger.LogError($"DIGITAL IDENTITY does not exist. Digital Identity GUID: {identityGuid}");
                return new NoContentResult();
            }
            else
            {
                _logger.LogInformation($"DIGITAL IDENTITY exists. Digital Identity GUID: {identityGuid}. Digital Identity ID: {digitalIdentity.IdentityID.Value}");
            }

            // LastLoggedInDateTime should be only updated when the user logs in using their digital account.
            if (digitalPatchRequest.LastLoggedInDateTime.HasValue && touchpointId != "0000000997")
            {
                _logger.LogError($"LastLoggedInDateTime value should be NULL. LastLoggedInDateTime: {digitalPatchRequest.LastLoggedInDateTime}. Touchpoint ID: {touchpointId}");
                return new UnprocessableEntityObjectResult("LastLoggedInDateTime should be null value.");
            }

            // Check if resource terminated
            if (digitalIdentity.DateOfClosure.HasValue && digitalIdentity.DateOfClosure.Value < DateTime.UtcNow)
            {
                _logger.LogError($"Cannot PATCH a DIGITAL IDENTITY which is terminated. Digital Identity GUID: {identityGuid}");
                return new UnprocessableEntityResult();
            }

            var model = _mapper.Map<Models.DigitalIdentity>(digitalIdentity);

            _logger.LogInformation($"Attempting to PATCH a DIGITAL IDENTITY. Digital Identity GUID: {identityGuid}");
            var patchedDigitalIdentity = await _identityPatchService.PatchAsync(model, digitalPatchRequest);

            if (patchedDigitalIdentity != null)
            {
                _logger.LogInformation($"PATCH request successful. Digital Identity ID: {digitalIdentity.IdentityID.Value}");
                _logger.LogInformation($"Function {nameof(PatchDigitalIdentityByIdentityIdHttpTrigger)} has finished invoking");

                return new JsonResult(patchedDigitalIdentity, new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            else
            {
                _logger.LogError($"PATCH request unsuccessful. Digital Identity ID: {digitalIdentity.IdentityID.Value}");
                _logger.LogInformation($"Function {nameof(PatchDigitalIdentityByIdentityIdHttpTrigger)} has finished invoking");

                // TODO - what status code should be returned in the event of failure?
                return new InternalServerErrorResult();
            }
        }
    }
}
