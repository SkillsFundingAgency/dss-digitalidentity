using AutoMapper;
using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.DigitalIdentity.Cosmos.Helper;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.DTO;
using NCS.DSS.DigitalIdentity.Interfaces;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Http;
using JsonException = Newtonsoft.Json.JsonException;


namespace NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Function
{
    public class PostDigitalIdentityHttpTrigger
    {
        private readonly IDigitalIdentityService _identityPostService;
        private readonly IDigitalIdentityServiceBusClient _serviceBusClient;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IDocumentDBProvider _provider;
        private readonly ILoggerHelper _loggerHelper;
        private readonly ILogger _logger;
        private readonly IValidate _validate;
        private readonly IMapper _mapper;
        private readonly IDynamicHelper _dynamicHelper;
        private static readonly string[] PropertyToExclude = {"TargetSite", "InnerException"};

        public PostDigitalIdentityHttpTrigger(
            IDigitalIdentityService identityPostService, 
            IDigitalIdentityServiceBusClient serviceBusClient,
            IHttpRequestHelper httpRequestHelper,
            IDocumentDBProvider provider,
            ILoggerHelper loggerHelper,
            ILogger<PostDigitalIdentityHttpTrigger> logger,
            IValidate validate,
            IMapper mapper,
            IDynamicHelper dynamicHelper)
        {
            _identityPostService = identityPostService;
            _serviceBusClient = serviceBusClient;
            _httpRequestHelper = httpRequestHelper;
            _provider = provider;
            _loggerHelper = loggerHelper;
            _logger = logger;
            _validate = validate;
            _mapper = mapper;
            _dynamicHelper = dynamicHelper;
        }

        [Function("POST")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity Created", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Post request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Digital Identity resource validation error(s)", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Conflict, Description = "Duplicate Email Address", ShowSchema = false)]
        [ProducesResponseType(typeof(Models.DigitalIdentity), (int)HttpStatusCode.OK)]
        [PostRequestBody(typeof(DigitalIdentityPost), "Digital Identity Request body")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "identity")] HttpRequest req)
        {
            DigitalIdentityPost identityRequest;
            _loggerHelper.LogMethodEnter(_logger);

            //Get Correlation Id
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
                //return new BadRequestResult();
            }

            _loggerHelper.LogInformationMessage(_logger, correlationGuid, "Apimurl:  " + apimUrl);

            _loggerHelper.LogInformationMessage(_logger, correlationGuid, string.Format("Post Digital Identity C# HTTP trigger function requested by Touchpoint: {0}", touchpointId));

            // Get request body
            try
            {
                identityRequest = await _httpRequestHelper.GetResourceFromRequest<DigitalIdentityPost>(req);
            }
            catch (JsonException ex)
            {
                _loggerHelper.LogError(_logger, correlationGuid, "Apimurl:  " + apimUrl, ex);
                return new UnprocessableEntityObjectResult(_dynamicHelper.ExcludeProperty(ex, PropertyToExclude));
            }

            if (identityRequest == null)
            {
                _loggerHelper.LogInformationMessage(_logger, correlationGuid, "digital identity post request is null");
                return new UnprocessableEntityResult();
            }

            if (identityRequest.CustomerId.Equals(Guid.Empty))
                return new UnprocessableEntityObjectResult("CustomerId is mandatory");

            if (identityRequest.DateOfClosure.HasValue)
                return new UnprocessableEntityObjectResult("Date of termination cannot be set in post request!");

            // Check if customer exists
            var doesCustomerExists = await _identityPostService.DoesCustomerExists(identityRequest.CustomerId);
            if (!doesCustomerExists)
                return new UnprocessableEntityObjectResult(
                    $"Customer with CustomerId  {identityRequest.CustomerId} does not exists.");

            var model = _mapper.Map<Models.DigitalIdentity>(identityRequest);

            var customer = await _provider.GetCustomer(identityRequest.CustomerId);
            var contact = await _provider.GetCustomerContact(identityRequest.CustomerId);
            model.SetCreateDigitalIdentity(contact?.EmailAddress, customer?.GivenName, customer?.FamilyName);

            //Customer exists check
            if (customer == null)
                return new UnprocessableEntityObjectResult(
                    $"Customer with CustomerId  {model.CustomerId} does not exists.");
            if (customer.DateOfTermination.HasValue)
                return new UnprocessableEntityObjectResult($"Customer with CustomerId  {model.CustomerId} is readonly");

            //only validate through posting a new digital identity 
            var digitalIdentity = await _provider.GetIdentityForCustomerAsync(model.CustomerId);
            if (digitalIdentity != null)
                return new UnprocessableEntityObjectResult(
                    $"Digital Identity for customer {model.CustomerId} already exists.");

            //email address check
            if (!string.IsNullOrEmpty(model.EmailAddress))
            {
                var doesContactWithEmailExists = await _provider.DoesContactDetailsWithEmailExists(model.EmailAddress);
                if (doesContactWithEmailExists)
                    return new UnprocessableEntityObjectResult(
                        $"Email address is already in use  {model.EmailAddress}.");
            }
            else
            {
                return new UnprocessableEntityObjectResult("Email address is required for customer.");
            }

            //Validate LastLoggedInDateTime, if not null return error message
            if (model.LastLoggedInDateTime is not null)
            {
                return new UnprocessableEntityObjectResult("LastLoggedInDateTime should be null value.");
            }

            // Validate request body
            var errors = await _validate.ValidateResource(identityRequest, true);

            if (errors != null && errors.Any())
                return new UnprocessableEntityObjectResult(errors);

            //Create digital Identity
            model.CreatedBy = touchpointId;
            model.LastModifiedTouchpointId = touchpointId;
            var createdIdentity = await _identityPostService.CreateAsync(model);
            var mappedIdentity = _mapper.Map<DigitalIdentityPost>(createdIdentity);

            // Notify service bus
            if (createdIdentity != null)
            {
                if (model.IsDigitalAccount == true)
                {
                    await _serviceBusClient.SendPostMessageAsync(model, apimUrl);
                }

                // return response
                return new JsonResult(mappedIdentity, new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.Created
                };
            }

            _loggerHelper.LogError(_logger, correlationGuid, $"Error creating resource.", null);
            return new InternalServerErrorResult();
        }
    }
}
