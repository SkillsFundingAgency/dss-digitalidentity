using System;
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
using NCS.DSS.DigitalIdentity.DeleteDigitalIdentityHttpTrigger.Service;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentityHttpTrigger.Function
{
    public static class DeleteDigitalIdentityHttpTrigger
    {
        [FunctionName("Delete")]
        [ResponseType(typeof(Models.DigitalIdentity))]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity found", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Digital Identity does not exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Request was malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Display(Name = "Delete", Description = "Ability to delete an individual digital identity")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "identity/{identityId}")]HttpRequest req, ILogger log, string identityId,
         //[Inject]IResourceHelper resourceHelper,
         [Inject]IDeleteDigitalIdentityHttpTriggerService identityDeleteService,
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

            if (!Guid.TryParse(identityId, out var identityGuid))
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Unable to parse 'identityId' to a Guid: {0}", identityId));
                return httpResponseMessageHelper.BadRequest(identityGuid);
            }
            //Do I validate if record exists first?

            var identityDeleted = await identityDeleteService.DeleteIdentityAsync(identityGuid);

            return !identityDeleted ?
                httpResponseMessageHelper.BadRequest(identityGuid) :
                httpResponseMessageHelper.Ok();
        }
    }
}
