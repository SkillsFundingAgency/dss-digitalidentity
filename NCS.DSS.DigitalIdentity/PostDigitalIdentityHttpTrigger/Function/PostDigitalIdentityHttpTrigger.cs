using DFC.Common.Standard.Logging;
using DFC.Functions.DI.Standard.Attributes;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.Validation;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Function
{
    public static class PostDigitalIdentityHttpTrigger
    {
        [FunctionName("POST")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity Created", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Post request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Digital Identity resource validation error(s)", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Conflict, Description = "Duplicate Email Address", ShowSchema = false)]
        [ProducesResponseType(typeof(Models.DigitalIdentity), (int)HttpStatusCode.OK)]
        public static async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "identity")] HttpRequest req, ILogger log,
            [Inject] IPostDigitalIdentityHttpTriggerService identityPostService,
            [Inject] ILoggerHelper loggerHelper,
            [Inject] IHttpRequestHelper httpRequestHelper,
            [Inject] IHttpResponseMessageHelper httpResponseMessageHelper,
            [Inject] IJsonHelper jsonHelper,
            [Inject] IValidate validate,
            [Inject] IDocumentDBProvider provider)
        {
            Models.DigitalIdentity identityRequest;
            loggerHelper.LogMethodEnter(log);

            //Get Correlation Id
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

            loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Post Digital Identity C# HTTP trigger function requested by Touchpoint: {0}", touchpointId));

            // Get request body
            try
            {
                identityRequest = await httpRequestHelper.GetResourceFromRequest<Models.DigitalIdentity>(req);
            }
            catch (JsonException ex)
            {
                loggerHelper.LogError(log, correlationGuid, "Apimurl:  " + apimUrl, ex);
                return httpResponseMessageHelper.UnprocessableEntity(ex);
            }

            if (identityRequest == null)
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, "digital identity post request is null");
                return httpResponseMessageHelper.UnprocessableEntity();
            }

            var customer = await provider.GetCustomer(identityRequest.CustomerId.Value);
            var contact = await provider.GetCustomerContact(identityRequest.CustomerId.Value);
            identityRequest.SetDigitalIdentity(contact?.EmailAddress, customer?.GivenName, customer?.FamilyName);

            // Validate request body
            var errors = await validate.ValidateResource(identityRequest, true);

            if (errors != null && errors.Any())
                return httpResponseMessageHelper.UnprocessableEntity(errors);

            // Create digital Identity
            identityRequest.CreatedBy = touchpointId;
            identityRequest.LastModifiedTouchpointId = touchpointId;
            var createdIdentity = await identityPostService.CreateAsync(identityRequest);

            // Notify service bus
            if (createdIdentity != null)
            {
                if(identityRequest.CreateDigitalIdentity == true)
                    await identityPostService.SendToServiceBusQueueAsync(identityRequest, apimUrl);

                // return response
                return httpResponseMessageHelper.Created(jsonHelper.SerializeObjectAndRenameIdProperty(identityRequest, "id", "IdentityID"));
            }
            else
            {
                loggerHelper.LogError(log, correlationGuid, $"Error creating resource.", null);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
    }
}
