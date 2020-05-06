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
using NCS.DSS.DigitalIdentity.GetDigitalIdentityByCustomerIdHttpTrigger.Service;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using Microsoft.AspNetCore.Http;
using DFC.Common.Standard.Logging;

namespace NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Function
{
    public static class PostDigitalIdentityHttpTrigger
    {
        [FunctionName("POST")]
        [ResponseType(typeof(Models.DigitalIdentity))]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity Added", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Request was malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Conflict, Description = "Digital Identity already exists for customer", ShowSchema = false)]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "/identity")]HttpRequest req, ILogger log, string customerId,
            //[Inject]IResourceHelper resourceHelper,
            [Inject]IGetDigitalIdentityByCustomerIdHttpTriggerService identityGetService,
            [Inject]ILoggerHelper loggerHelper,
            [Inject]IHttpRequestHelper httpRequestHelper,
            [Inject]IHttpResponseMessageHelper httpResponseMessageHelper,
            [Inject]IJsonHelper jsonHelper)
        {
            return httpResponseMessageHelper.Created(jsonHelper.SerializeObjectAndRenameIdProperty(new Models.DigitalIdentity(), "id", "DigitalIdentityId"));
        }
    }
}
