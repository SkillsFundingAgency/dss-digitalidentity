﻿using System;
using System.ComponentModel.DataAnnotations;
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
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using Microsoft.AspNetCore.Http;
using DFC.Common.Standard.Logging;
using NCS.DSS.DigitalIdentity.DeleteDigitalIdentityByCustomerIdHttpTrigger.Service;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentityByCustomerIdHttpTrigger.Function
{
    public static class DeleteDigitalIdentityByCustomerIdHttpTrigger
    {
        [FunctionName("DeleteById")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity found", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Digital Identity does not exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Request was malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Display(Name = "DeleteById", Description = "Ability to delete an individual digital identity by customer id")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customer/{customerId}")]HttpRequest req, ILogger log, string customerId,
         //[Inject]IResourceHelper resourceHelper,
         [Inject]IDeleteDigitalIdentityByCustomerIdHttpTriggerService identityDeleteService,
         [Inject]ILoggerHelper loggerHelper,
         [Inject]IHttpRequestHelper httpRequestHelper,
         [Inject]IHttpResponseMessageHelper httpResponseMessageHelper,
         [Inject]IJsonHelper jsonHelper)
        {

            loggerHelper.LogMethodEnter(log);

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

            var touchpointId = httpRequestHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, "Unable to locate 'TouchpointId' in request header");
                return httpResponseMessageHelper.BadRequest();
            }

            loggerHelper.LogInformationMessage(log, correlationGuid,
                string.Format("Get Digital Identity By Id C# HTTP trigger function  processed a request. By Touchpoint: {0}",
                    touchpointId));

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Unable to parse 'customerId' to a Guid: {0}", customerId));
                return httpResponseMessageHelper.BadRequest(customerGuid);
            }

            //First get the identity from the customer id
            loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Attempting to get identity for customer {0}", customerGuid));
            var identity = await identityDeleteService.GetIdentityForCustomerAsync(customerGuid);

            //Then delete using that id
            loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Attempting to delete identity {0}", identity.IdentityID.Value));
            var identityDeleted = await identityDeleteService.DeleteIdentityAsync(identity.IdentityID.Value);

            return !identityDeleted ?
                httpResponseMessageHelper.BadRequest(customerGuid) :
                httpResponseMessageHelper.Ok();
        }
    }
}