using AutoMapper;
using DFC.Common.Standard.Logging;
using DFC.Functions.DI.Standard.Attributes;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NCS.DSS.DigitalIdentity.DTO;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.Interfaces;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.PatchDigitalIdentityHttpTrigger.Function
{
    public static class PatchDigitalIdentityByCustomerIdHttpTrigger
    {
        [FunctionName("PatchByCustomerId")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity Patched", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Patch request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Digital Identity resource validation error(s)", ShowSchema = false)]
        [ProducesResponseType(typeof(Models.DigitalIdentity), (int)HttpStatusCode.OK)]
        [PostRequestBody(typeof(DigitalIdentityPatch), "Digital Identity Request body")]
        public static async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "customer/{customerId}")] HttpRequest req, ILogger log,
            string customerId,
            [Inject]IDigitalIdentityService identityPatchService,
            [Inject]IGetDigitalIdentityHttpTriggerService identityGetService,
            [Inject]ILoggerHelper loggerHelper,
            [Inject]IHttpRequestHelper httpRequestHelper,
            [Inject]IHttpResponseMessageHelper httpResponseMessageHelper,
            [Inject]IJsonHelper jsonHelper,
            [Inject]IValidate validate,
            [Inject] IMapper mapper,
            [Inject] IConfiguration config)
        {
            loggerHelper.LogMethodEnter(log);
            var touchPointsPermittedToUpdateLastLoggedIn = config["TouchPointsPermittedToUpdateLastLoggedIn"]?.Split(",") ?? new string[0];

            // Get Correlation Id
            var correlationId = httpRequestHelper.GetDssCorrelationId(req);
            if (string.IsNullOrEmpty(correlationId))
            {
                log.LogInformation("Unable to locate 'DssCorrelationId' in request header");
            }

            if (!Guid.TryParse(correlationId, out var correlationGuid))
            {
                log.LogInformation("Unable to parse 'DssCorrelationId' to a Guid");
                correlationGuid = Guid.NewGuid();
            }

            // Get Touch point Id
            var touchpointId = httpRequestHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, "Unable to locate 'TouchpointId' in request header");
                return httpResponseMessageHelper.BadRequest();
            }

            // Get Apim URL
            var apimUrl = httpRequestHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(apimUrl))
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, "Unable to locate 'apimurl' in request header");
                return httpResponseMessageHelper.BadRequest();
            }

            loggerHelper.LogInformationMessage(log, correlationGuid, "Apimurl:  " + apimUrl);

            loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Patch Digital Identity C# HTTP trigger function requested by Touchpoint: {0}", touchpointId));

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Unable to parse 'customerId' to a Guid: {0}", customerId));
                return httpResponseMessageHelper.BadRequest(customerGuid);
            }

            // Get patch body
            DigitalIdentityPatch digitalPatchRequest;
            try
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, "Attempt to get resource from body of the request");
                digitalPatchRequest = await httpRequestHelper.GetResourceFromRequest<DigitalIdentityPatch>(req);
            }
            catch (JsonException ex)
            {
                loggerHelper.LogError(log, correlationGuid, "Unable to retrieve body from req", ex);
                return httpResponseMessageHelper.UnprocessableEntity(ex);
            }

            if (digitalPatchRequest == null)
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, "digital identity patch request is null");
                return httpResponseMessageHelper.UnprocessableEntity();
            }

            // Validate patch body
            loggerHelper.LogInformationMessage(log, correlationGuid, "Attempt to validate resource");
            var errors = await validate.ValidateResource(digitalPatchRequest, false);

            if (errors != null && errors.Any())
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, "validation errors with resource");
                return httpResponseMessageHelper.UnprocessableEntity(errors);
            }

            digitalPatchRequest.LastModifiedTouchpointId = touchpointId;

            // Check if customer exists
            var doesCustomerExists = await identityPatchService.DoesCustomerExists(customerGuid);

            if (!doesCustomerExists)
                    return httpResponseMessageHelper.UnprocessableEntity(
                        $"Customer with CustomerId  {digitalPatchRequest.CustomerId} does not exists.");


            // Check if identity resource exists for customer

            loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Attempting to get Digital Identity by Customer Id {0}", customerGuid));
            var digitalIdentity = await identityGetService.GetIdentityForCustomerAsync(customerGuid);

            if (digitalIdentity == null)
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Unable to get Digital Identity resource {0}", customerGuid));
                return httpResponseMessageHelper.NoContent(customerGuid);
            }

            if (digitalPatchRequest.LastLoggedInDateTime.HasValue && !touchPointsPermittedToUpdateLastLoggedIn.Contains(touchpointId))
            {
                return httpResponseMessageHelper.UnprocessableEntity($"LastLoggedInDateTime is readonly");
            }

            // Check if resource terminated
            if (digitalIdentity.DateOfClosure.HasValue && digitalIdentity.DateOfClosure.Value < DateTime.UtcNow)
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Patch requested on terminated resource {0}", customerGuid));
                return httpResponseMessageHelper.UnprocessableEntity();
            }

            var model = mapper.Map<Models.DigitalIdentity>(digitalIdentity);

            loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Attempting to patch identity resource {0}", customerGuid));
            var patchedCustomer = await identityPatchService.PatchAsync(model, digitalPatchRequest);
            return httpResponseMessageHelper.Ok(jsonHelper.SerializeObjectAndRenameIdProperty(patchedCustomer, "id", "IdentityID"));
        }
    }
}
