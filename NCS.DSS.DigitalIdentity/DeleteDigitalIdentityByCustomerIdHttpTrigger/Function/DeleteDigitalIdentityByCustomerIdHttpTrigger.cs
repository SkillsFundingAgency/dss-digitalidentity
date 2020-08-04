using DFC.Common.Standard.Logging;
using DFC.Functions.DI.Standard.Attributes;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NCS.DSS.DigitalIdentity.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentityByCustomerIdHttpTrigger.Function
{
    public class DeleteDigitalIdentityByCustomerIdHttpTrigger
    {
        private readonly IDigitalIdentityService _identityDeleteService;
        private readonly IDigitalIdentityServiceBusClient _serviceBus;
        private readonly ILoggerHelper _loggerHelper;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IHttpResponseMessageHelper _httpResponseMessageHelper;

        public DeleteDigitalIdentityByCustomerIdHttpTrigger(IDigitalIdentityService deleteService, IDigitalIdentityServiceBusClient serviceBus, ILoggerHelper loggerHelper, IHttpRequestHelper httpRequestHelper, IHttpResponseMessageHelper httpResponseMessageHelper)
        {
            _identityDeleteService = deleteService;
            _serviceBus = serviceBus;
            _loggerHelper = loggerHelper;
            _httpRequestHelper = httpRequestHelper;
            _httpResponseMessageHelper = httpResponseMessageHelper;
        }

        [FunctionName("DeleteById")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity found", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Digital Identity does not exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Request was malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Display(Name = "DeleteById", Description = "Ability to delete an individual digital identity by customer id")]
        public async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customer/{customerId}")] HttpRequest req, ILogger _logger, string customerId)
        {
            var apimUrl = _httpRequestHelper.GetDssApimUrl(req);
            var touchpointId = _httpRequestHelper.GetDssTouchpointId(req);
            var correlationId = _httpRequestHelper.GetDssCorrelationId(req);

            _loggerHelper.LogMethodEnter(_logger);


            if (string.IsNullOrEmpty(correlationId))
            {
                _logger.LogInformation("Unable to locate 'DssCorrelationId' in request header");
            }

            if (!Guid.TryParse(correlationId, out var correlationGuid))
            {
                _logger.LogInformation("Unable to parse 'DssCorrelationId' to a Guid");
                correlationGuid = Guid.NewGuid();
            }

            if (string.IsNullOrEmpty(touchpointId))
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, "Unable to locate 'TouchpointId' in request header");
                return _httpResponseMessageHelper.BadRequest();
            }

            _loggerHelper.LogInformationMessage(_logger, correlationGuid,
                string.Format("Get Digital Identity By Id C# HTTP trigger function  processed a request. By Touchpoint: {0}",
                    touchpointId));

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Unable to parse 'customerId' to a Guid: {0}", customerId));
                return _httpResponseMessageHelper.BadRequest(customerGuid);
            }

            var customerExists = await _identityDeleteService.DoesCustomerExists(customerGuid);
            if(!customerExists)
                return _httpResponseMessageHelper.BadRequest();

            //First get the identity from the customer id
            _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Attempting to get identity for customer {0}", customerGuid));
            var identity = await _identityDeleteService.GetIdentityForCustomerAsync(customerGuid);

            if (identity == null)
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, $"Cannot delete digital identity for customer: {customerId} , customer does not have a digital identity");
                return _httpResponseMessageHelper.NoContent($"{customerId} does not have a digital identity");
            }

            //trigger change feed
            identity.DateOfClosure = DateTime.Now;
            identity.LastModifiedTouchpointId = touchpointId;
            await _identityDeleteService.UpdateASync(identity);

            _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Attempting to delete identity {0}", identity?.IdentityID.Value));
            var identityDeleted = await _identityDeleteService.DeleteIdentityAsync(identity.IdentityID.Value);

            if (identityDeleted)
            {
                identity.SetDeleted();
                await _serviceBus.SendDeleteMessageAsync(identity, apimUrl);
            }


            return !identityDeleted ?
                _httpResponseMessageHelper.BadRequest(customerGuid) :
                _httpResponseMessageHelper.Ok();
        }
    }
}
