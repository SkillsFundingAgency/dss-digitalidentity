﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http.Description;
using Microsoft.Extensions.Logging;
using DFC.Swagger.Standard.Annotations;
using DFC.Functions.DI.Standard.Attributes;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityByCustomerIdHttpTrigger.Service;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using Microsoft.AspNetCore.Http;
using DFC.Common.Standard.Logging;
using Microsoft.AspNetCore.Mvc;
using NCS.DSS.DigitalIdentity.Cosmos.Helper;
using NCS.DSS.DigitalIdentity.PatchDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.Validation;
using Newtonsoft.Json;

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
        public static async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "customer/{customerId}")] HttpRequest req, ILogger log,
            string customerId,
            [Inject]IResourceHelper resourceHelper,
            [Inject]IPatchDigitalIdentityHttpTriggerService identityPatchService,
            [Inject]IGetDigitalIdentityByCustomerIdHttpTriggerService identityGetService,
            [Inject]ILoggerHelper loggerHelper,
            [Inject]IHttpRequestHelper httpRequestHelper,
            [Inject]IHttpResponseMessageHelper httpResponseMessageHelper,
            [Inject]IJsonHelper jsonHelper,
            [Inject]IValidate validate)
        {
            loggerHelper.LogMethodEnter(log);

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
            Models.DigitalIdentityPatch digitalPatchRequest;
            try
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, "Attempt to get resource from body of the request");
                digitalPatchRequest = await httpRequestHelper.GetResourceFromRequest<Models.DigitalIdentityPatch>(req);
            }
            catch (JsonException ex)
            {
                loggerHelper.LogError(log, correlationGuid, "Unable to retrieve body from req", ex);
                return httpResponseMessageHelper.UnprocessableEntity(ex);
            }

            if (digitalPatchRequest == null)
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, "digital identity patch request is null");
                return httpResponseMessageHelper.UnprocessableEntity(req);
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

            // Check if identity resource exists
            loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Attempting to get Digital Identity by Customer Id {0}", customerGuid));
            var digitalIdentity = await identityGetService.GetIdentityForCustomerAsync(customerGuid);

            if (digitalIdentity == null)
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Unable to get Digital Identity resource {0}", customerGuid));
                return httpResponseMessageHelper.NoContent(customerGuid);
            }

            // Check if resource terminated
            if (digitalIdentity.DateOfTermination.HasValue && digitalIdentity.DateOfTermination.Value < DateTime.UtcNow)
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Patch requested on terminated resource {0}", customerGuid));
                return httpResponseMessageHelper.NoContent(customerGuid);
            }

            loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Attempting to patch identity resource {0}", customerGuid));
            var patchedCustomer = await identityPatchService.UpdateIdentity(digitalIdentity, digitalPatchRequest);

            // TODO : Enable this when service bus is created
            // Notify service bus
            //if (patchedCustomer != null)
            //    await identityPatchService.SendToServiceBusQueueAsync(patchedCustomer, apimUrl);

            return httpResponseMessageHelper.Created(jsonHelper.SerializeObjectAndRenameIdProperty(new Models.DigitalIdentity(), "id", "DigitalIdentityId"));
        }
    }
}