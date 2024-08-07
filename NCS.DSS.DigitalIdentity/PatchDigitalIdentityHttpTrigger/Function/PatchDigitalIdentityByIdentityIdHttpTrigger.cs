using AutoMapper;
using DFC.Common.Standard.Logging;
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
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.PatchDigitalIdentityHttpTrigger.Function
{
    public class PatchDigitalIdentityByIdentityIdHttpTrigger
    {
        private readonly IDigitalIdentityService _identityPatchService;
        private readonly IGetDigitalIdentityHttpTriggerService _identityGetService;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IValidate _validate;
        private readonly ILoggerHelper _loggerHelper;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IDynamicHelper _dynamicHelper;
        private static readonly string[] PropertyToExclude = {"TargetSite"};

        public PatchDigitalIdentityByIdentityIdHttpTrigger(
            IDigitalIdentityService identityPatchService, 
            IGetDigitalIdentityHttpTriggerService identityGetService,
            IHttpRequestHelper httpRequestHelper,
            IValidate validate,
            ILoggerHelper loggerHelper,
            ILogger<PatchDigitalIdentityByIdentityIdHttpTrigger> logger,
            IMapper mapper,
            IDynamicHelper dynamicHelper)
        {
            _identityPatchService = identityPatchService;
            _identityGetService = identityGetService;
            _httpRequestHelper = httpRequestHelper;
            _validate = validate;
            _loggerHelper = loggerHelper;
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
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "identity/{IdentityId}")] HttpRequest req, string IdentityId)
        {
            _loggerHelper.LogMethodEnter(_logger);

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
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, "Unable to locate 'TouchpointId' in request header");
                return new BadRequestResult();
            }

            // Get Apim URL
            var apimUrl = _httpRequestHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(apimUrl))
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, "Unable to locate 'apimurl' in request header");
                return new BadRequestResult();
            }

            _loggerHelper.LogInformationMessage(_logger, correlationGuid, "Apimurl:  " + apimUrl);

            _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Patch Digital Identity C# HTTP trigger function requested by Touchpoint: {0}", touchpointId));

            if (!Guid.TryParse(IdentityId, out var identityGuid))
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Unable to parse 'IdentityId' to a Guid: {0}", IdentityId));
                return new BadRequestObjectResult(identityGuid.ToString());
            }

            // Get patch body
            DigitalIdentityPatch digitalPatchRequest;
            try
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, "Attempt to get resource from body of the request");
                digitalPatchRequest = await _httpRequestHelper.GetResourceFromRequest<DigitalIdentityPatch>(req);
            }
            catch (JsonException ex)
            {
                _loggerHelper.LogError(_logger, correlationGuid, "Unable to retrieve body from req", ex);
                return new UnprocessableEntityObjectResult(_dynamicHelper.ExcludeProperty(ex, PropertyToExclude));
            }

            if (digitalPatchRequest == null)
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, "digital identity patch request is null");
                return new UnprocessableEntityResult();
            }

            // Validate patch body
            _loggerHelper.LogInformationMessage(_logger, correlationGuid, "Attempt to validate resource");
            var errors = await _validate.ValidateResource(digitalPatchRequest, false);

            if (errors != null && errors.Any())
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, "validation errors with resource");
                return new UnprocessableEntityObjectResult(errors);
            }

            // Check if customer exists then validate
            if (!(digitalPatchRequest.CustomerId.Equals(Guid.Empty)))
            {
                var doesCustomerExists = await _identityPatchService.DoesCustomerExists(digitalPatchRequest.CustomerId);

                if (!doesCustomerExists)
                    return new UnprocessableEntityObjectResult(
                        $"Customer with CustomerId  {digitalPatchRequest.CustomerId} does not exists.");
            }

            digitalPatchRequest.LastModifiedTouchpointId = touchpointId;

            // Check if resource exists
            _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Attempting to get Digital Identity by Identity Id {0}", identityGuid));
            var digitalIdentity = await _identityGetService.GetIdentityAsync(identityGuid);

            if (digitalIdentity == null)
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Unable to get Digital Identity resource {0}", identityGuid));
                return new ObjectResult(identityGuid.ToString())
                {
                    StatusCode = (int)HttpStatusCode.NoContent
                };
            }

            //LastLoggedInDateTime should be only updated when the user logs in using their digitalaccount.
            if (digitalPatchRequest.LastLoggedInDateTime.HasValue && touchpointId != "0000000997")
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("LastLoggedInDateTime  {0} and touchpoint {1}", digitalPatchRequest.LastLoggedInDateTime, touchpointId));
                return new UnprocessableEntityObjectResult("LastLoggedInDateTime should be null value.");
            }

            // Check if resource terminated
            if (digitalIdentity.DateOfClosure.HasValue && digitalIdentity.DateOfClosure.Value < DateTime.UtcNow)
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Patch requested on terminated resource {0}", identityGuid));
                return new UnprocessableEntityResult();
            }
            var model = _mapper.Map<Models.DigitalIdentity>(digitalIdentity);
            _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Attempting to patch identity resource {0}", identityGuid));

            await _identityPatchService.PatchAsync(model, digitalPatchRequest);

            return new OkResult();
        }
    }
}
