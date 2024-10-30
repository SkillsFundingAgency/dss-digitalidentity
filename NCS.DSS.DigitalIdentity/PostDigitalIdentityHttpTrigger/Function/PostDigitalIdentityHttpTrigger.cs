using AutoMapper;
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
using System.Net;
using System.Text.Json;
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
        private readonly ILogger<PostDigitalIdentityHttpTrigger> _logger;
        private readonly IValidate _validate;
        private readonly IMapper _mapper;
        private readonly IDynamicHelper _dynamicHelper;
        private static readonly string[] PropertyToExclude = { "TargetSite", "InnerException" };

        public PostDigitalIdentityHttpTrigger(
            IDigitalIdentityService identityPostService,
            IDigitalIdentityServiceBusClient serviceBusClient,
            IHttpRequestHelper httpRequestHelper,
            IDocumentDBProvider provider,
            ILogger<PostDigitalIdentityHttpTrigger> logger,
            IValidate validate,
            IMapper mapper,
            IDynamicHelper dynamicHelper)
        {
            _identityPostService = identityPostService;
            _serviceBusClient = serviceBusClient;
            _httpRequestHelper = httpRequestHelper;
            _provider = provider;
            _logger = logger;
            _validate = validate;
            _mapper = mapper;
            _dynamicHelper = dynamicHelper;
        }

        [Function("Post")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Digital Identity Created", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Post request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Digital Identity resource validation error(s)", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Conflict, Description = "Duplicate Email Address", ShowSchema = false)]
        [ProducesResponseType(typeof(Models.DigitalIdentity), (int)HttpStatusCode.OK)]
        [PostRequestBody(typeof(DigitalIdentityPost), "Digital Identity Request body")]
        //[Display(Name = "Post", Description = "Lorum ipsum - what do I do?")] --> should I have this?
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "identity")] HttpRequest req)
        {
            _logger.LogInformation($"Function {nameof(PostDigitalIdentityHttpTrigger)} has been invoked");

            DigitalIdentityPost identityRequest;

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
                _logger.LogInformation($"Unable to locate 'TouchpointId' in request header. Correlation GUID: {correlationGuid}");
                return new BadRequestResult();
            }

            // Get Apim URL
            var apimUrl = _httpRequestHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(apimUrl))
            {
                _logger.LogInformation($"Unable to locate 'apimurl' in request header. Correlation GUID: {correlationGuid}");
                return new BadRequestResult();
            }

            _logger.LogInformation($"APIM URL: {apimUrl}");
            _logger.LogInformation($"Header validation has succeeded. Touchpoint ID: {touchpointId}. Correlation GUID: {correlationGuid}");
            _logger.LogInformation($"Attempting to get resource from request body. Correlation GUID: {correlationGuid}");

            // Get request body
            try
            {
                identityRequest = await _httpRequestHelper.GetResourceFromRequest<DigitalIdentityPost>(req);
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Unable to parse DigitalIdentityPost from request body. Correlation GUID: {correlationGuid}. Exception: {ex.Message}");
                return new UnprocessableEntityObjectResult(_dynamicHelper.ExcludeProperty(ex, PropertyToExclude));
            }

            if (identityRequest == null)
            {
                _logger.LogError($"DigitalIdentityPost object is NULL. Correlation GUID: {correlationGuid}");
                return new UnprocessableEntityResult();
            }

            if (identityRequest.CustomerId.Equals(Guid.Empty))
            {
                _logger.LogError("CustomerId is missing from the DigitalIdentityPost object - it is mandatory.");
                return new UnprocessableEntityObjectResult("CustomerId is mandatory");
            }
                
            if (identityRequest.DateOfClosure.HasValue)
            {
                _logger.LogError("Cannot create a DIGITAL IDENTITY with a termination date - remove it from the DigitalIdentityPost object.");
                return new UnprocessableEntityObjectResult("Date of termination cannot be set in post request!");
            }
                
            // Check if customer exists
            var doesCustomerExists = await _identityPostService.DoesCustomerExists(identityRequest.CustomerId);
            
            if (doesCustomerExists)
            {
                _logger.LogInformation($"Customer exists. Customer ID: {identityRequest.CustomerId}");
            }
            else
            {
                _logger.LogError($"Customer does not exist. Customer ID: {identityRequest.CustomerId}");
                return new UnprocessableEntityObjectResult($"Customer with CustomerId  {identityRequest.CustomerId} does not exists.");
            }

            _logger.LogInformation($"Attempting to retrieve Customer. Customer ID: {identityRequest.CustomerId}");

            var customer = await _provider.GetCustomer(identityRequest.CustomerId);

            if (customer == null)
            {
                _logger.LogError($"Failed to retrieve customer information. Customer ID: {identityRequest.CustomerId}");
                return new UnprocessableEntityObjectResult($"Customer with CustomerId  {identityRequest.CustomerId} does not exists.");
            } 
            
            if (customer.DateOfTermination.HasValue)
            {
                _logger.LogError($"Cannot POST a DIGITAL IDENTITY for a Customer which is terminated. Customer ID: {identityRequest.CustomerId}");
                return new UnprocessableEntityObjectResult($"Customer with CustomerId  {identityRequest.CustomerId} is readonly");
            }

            _logger.LogInformation($"Attempting to retrieve Contact Details for a Customer. Customer ID: {identityRequest.CustomerId}");

            var contact = await _provider.GetCustomerContact(identityRequest.CustomerId);

            if (contact == null)
            {
                _logger.LogInformation($"No Contact Details exist for Customer ID: {identityRequest.CustomerId}");
            }

            var model = _mapper.Map<Models.DigitalIdentity>(identityRequest);
            model.SetCreateDigitalIdentity(contact?.EmailAddress, customer?.GivenName, customer?.FamilyName);

            _logger.LogInformation($"Attempting to retrieve an existing DIGITAL IDENTITY for a Customer. Customer ID: {model.CustomerId}");
            var digitalIdentity = await _provider.GetIdentityForCustomerAsync(model.CustomerId);

            if (digitalIdentity != null)
            {
                _logger.LogError($"A DIGITAL IDENTITY for Customer ID '{model.CustomerId}' already exists. Digital Identity ID: {digitalIdentity.IdentityID.Value}");
                return new UnprocessableEntityObjectResult($"Digital Identity for customer {model.CustomerId} already exists.");
            }

            if (!string.IsNullOrEmpty(model.EmailAddress))
            {
                var digitalIdentityWithEmailAddressAlreadyExists = await _provider.GetDigitalIdentityForAnEmailAddress(model.EmailAddress);
                if (digitalIdentityWithEmailAddressAlreadyExists)
                {
                    _logger.LogError($"A DIGITAL IDENTITY for email address '{model.EmailAddress}' already exists");
                    return new UnprocessableEntityObjectResult($"Email address is already in use {model.EmailAddress}.");
                }
            }
            else
            {
                _logger.LogError("An email address is required for a Customer.");
                return new UnprocessableEntityObjectResult("Email address is required for customer.");
            }

            // Validate LastLoggedInDateTime, if not null return error message
            if (model.LastLoggedInDateTime is not null)
            {
                _logger.LogError($"LastLoggedInDateTime value should be NULL. LastLoggedInDateTime: {model.LastLoggedInDateTime}. Touchpoint ID: {touchpointId}");
                return new UnprocessableEntityObjectResult("LastLoggedInDateTime should be null value.");
            }

            // Validate request body
            _logger.LogInformation($"Attempting to validate DigitalIdentityPost object. Correlation GUID: {correlationGuid}");
            var errors = await _validate.ValidateResource(identityRequest, true);

            if (errors != null && errors.Any())
            {
                _logger.LogError($"Validation for DigitalIdentityPost object failed. Correlation GUID: {correlationGuid}");
                return new UnprocessableEntityObjectResult(errors);
            }

            // Create digital Identity
            model.CreatedBy = touchpointId;
            model.LastModifiedTouchpointId = touchpointId;

            _logger.LogInformation("Attempting to POST a DIGITAL IDENTITY");
            var createdIdentity = await _identityPostService.CreateAsync(model);
            
            if (createdIdentity != null)
            {
                _logger.LogInformation($"POST to DIGITAL IDENTITY was successful. Digital Identity ID: {createdIdentity.IdentityID.Value}");

                if (model.IsDigitalAccount == true)
                {
                    _logger.LogInformation($"Attempting to send creation notification to Service Bus Namespace (Digital Account)");
                    await _serviceBusClient.SendPostMessageAsync(model, apimUrl);
                }

                _logger.LogInformation($"Function {nameof(PostDigitalIdentityHttpTrigger)} has finished invoking");

                return new JsonResult(_mapper.Map<DigitalIdentityPost>(createdIdentity), new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.Created
                };
            }

            _logger.LogError("Unable to create new DIGITAL IDENTITY");
            _logger.LogInformation($"Function {nameof(PostDigitalIdentityHttpTrigger)} has finished invoking");

            return new InternalServerErrorResult();
        }
    }
}
