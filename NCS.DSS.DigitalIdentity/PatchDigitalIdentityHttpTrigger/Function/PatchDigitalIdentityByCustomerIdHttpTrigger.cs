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
    public class PatchDigitalIdentityByCustomerIdHttpTrigger
    {
        private readonly IDigitalIdentityService _identityPatchService;
        private readonly IGetDigitalIdentityHttpTriggerService _identityGetService;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IValidate _validate;
        private readonly ILogger<PatchDigitalIdentityByCustomerIdHttpTrigger> _logger;
        private readonly IMapper _mapper;
        private readonly IDynamicHelper _dynamicHelper;
        private static readonly string[] PropertyToExclude = { "TargetSite" };

        public PatchDigitalIdentityByCustomerIdHttpTrigger(
            IDigitalIdentityService identityPatchService,
            IGetDigitalIdentityHttpTriggerService identityGetService,
            IHttpRequestHelper httpRequestHelper,
            IValidate validate,
            ILogger<PatchDigitalIdentityByCustomerIdHttpTrigger> logger,
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

        [Function("PatchByCustomerId")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity Patched", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Patch request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Digital Identity resource validation error(s)", ShowSchema = false)]
        [ProducesResponseType(typeof(Models.DigitalIdentity), (int)HttpStatusCode.OK)]
        [PostRequestBody(typeof(DigitalIdentityPatch), "Digital Identity Request body")]
        //[Display(Name = "PatchByCustomerId", Description = "Lorum ipsum - what do I do?")] --> should I have this?
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "customer/{customerId}")] HttpRequest req, string customerId)
        {
            _logger.LogInformation($"Function {nameof(PatchDigitalIdentityByCustomerIdHttpTrigger)} has been invoked");

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

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                _logger.LogInformation($"Unable to parse 'customerId' to a GUID. Customer ID: {customerId}");
                return new BadRequestObjectResult(customerGuid.ToString());
            }

            _logger.LogInformation($"Header validation has succeeded. Touchpoint ID: {touchpointId}. Correlation GUID: {correlationGuid}");
            _logger.LogInformation($"Attempting to get resource from request body. Correlation GUID: {correlationGuid}");

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

            digitalPatchRequest.LastModifiedTouchpointId = touchpointId;

            // Check if customer exists
            var doesCustomerExists = await _identityPatchService.DoesCustomerExists(customerGuid);

            if (doesCustomerExists)
            {
                _logger.LogInformation($"Customer exists. Customer ID: {digitalPatchRequest.CustomerId}. Customer GUID: {customerGuid}");
            }
            else
            {
                _logger.LogError($"Customer does not exist. Customer ID: {digitalPatchRequest.CustomerId}. Customer GUID: {customerGuid}");
                return new UnprocessableEntityObjectResult($"Customer with CustomerId  {digitalPatchRequest.CustomerId} does not exists.");
            }

            // Check if identity resource exists for customer
            _logger.LogInformation($"Attempting to retrieve DIGITAL IDENTITY for Customer. Customer GUID: {customerGuid}");
            var digitalIdentity = await _identityGetService.GetIdentityForCustomerAsync(customerGuid);

            if (digitalIdentity == null)
            {
                _logger.LogError($"DIGITAL IDENTITY does not exist for Customer. Customer GUID: {customerGuid}");
                return new NoContentResult();
            }

            _logger.LogInformation($"DIGITAL IDENTITY exists for Customer. Customer GUID: {customerGuid}. Digital Identity ID: {digitalIdentity.IdentityID.Value}");

            // LastLoggedInDateTime should be only updated when the user logs in using their digital account.
            if (digitalPatchRequest.LastLoggedInDateTime.HasValue && touchpointId != "0000000997")
            {
                _logger.LogError($"LastLoggedInDateTime value should be NULL. LastLoggedInDateTime: {digitalPatchRequest.LastLoggedInDateTime}. Touchpoint ID: {touchpointId}");
                return new UnprocessableEntityObjectResult($"LastLoggedInDateTime should be null value.");
            }

            // Check if resource terminated
            if (digitalIdentity.DateOfClosure.HasValue && digitalIdentity.DateOfClosure.Value < DateTime.UtcNow)
            {
                _logger.LogError($"Cannot PATCH a DIGITAL IDENTITY which is terminated. Customer GUID: {customerGuid}. Digital Identity ID: {digitalIdentity.IdentityID.Value}");
                return new UnprocessableEntityResult();
            }

            var model = _mapper.Map<Models.DigitalIdentity>(digitalIdentity);

            _logger.LogInformation($"Attempting to PATCH a DIGITAL IDENTITY. Customer GUID: {customerGuid}");
            var patchedDigitalIdentity = await _identityPatchService.PatchAsync(model, digitalPatchRequest);

            if (patchedDigitalIdentity != null)
            {
                _logger.LogInformation($"PATCH request successful. Digital Identity ID: {digitalIdentity.IdentityID.Value}");
                _logger.LogInformation($"Function {nameof(PatchDigitalIdentityByCustomerIdHttpTrigger)} has finished invoking");

                return new JsonResult(patchedDigitalIdentity, new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.OK
                };
            } 
            else
            {
                _logger.LogError($"PATCH request unsuccessful. Digital Identity ID: {digitalIdentity.IdentityID.Value}");
                _logger.LogInformation($"Function {nameof(PatchDigitalIdentityByCustomerIdHttpTrigger)} has finished invoking");

                // TODO - what status code should be returned in the event of failure?
                return new InternalServerErrorResult();
            }
        }
    }
}
