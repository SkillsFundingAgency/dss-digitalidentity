using System;
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
using NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.Validation;
using Newtonsoft.Json;

namespace NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Function
{
    public static class PostDigitalIdentityHttpTrigger
    {
        [FunctionName("POST")]

        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity Added", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Post request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Digital Identity resource validation error(s)", ShowSchema = false)]
        [ProducesResponseType(typeof(Models.DigitalIdentity), (int)HttpStatusCode.OK)]
        public static async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "identity")]HttpRequest req, ILogger log,
            [Inject]IResourceHelper resourceHelper,
            [Inject]IPostDigitalIdentityHttpTriggerService identityPostService,
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

            loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Post Digital Identity C# HTTP trigger function requested by Touchpoint: {0}", touchpointId));

            // Get request body
            Models.DigitalIdentity identityRequest;
            try
            {
                identityRequest = await httpRequestHelper.GetResourceFromRequest<Models.DigitalIdentity>(req);
            }
            catch (JsonException ex)
            {
                return httpResponseMessageHelper.UnprocessableEntity(ex);
            }

            // Validate request body
            var errors = await validate.ValidateResource(identityRequest, true);

            if (errors != null && errors.Any())
                return httpResponseMessageHelper.UnprocessableEntity(errors);

            // Create digital Identity
            var createdIdentity = await identityPostService.CreateAsync(identityRequest);

            // Notify service bus
            if (createdIdentity != null)
                await identityPostService.SendToServiceBusQueueAsync(createdIdentity, apimUrl);

            // return response
            return httpResponseMessageHelper.Created(jsonHelper.SerializeObjectAndRenameIdProperty(createdIdentity, "id", "DigitalIdentityId"));
        }
    }
}
