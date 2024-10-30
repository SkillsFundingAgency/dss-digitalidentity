using AutoMapper;
using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.DigitalIdentity.Cosmos.Helper;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.DTO;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.Interfaces;
using NCS.DSS.DigitalIdentity.Mappings;
using NCS.DSS.DigitalIdentity.Models;
using NCS.DSS.DigitalIdentity.PatchDigitalIdentityHttpTrigger.Function;
using NCS.DSS.DigitalIdentity.Services;
using NCS.DSS.DigitalIdentity.Validation;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.UnitTests
{
    [TestFixture]
    public class PatchDigitalIdentityByCustomerIdHttpTriggerTests
    {
        private const string TouchpointIdHeaderParamKey = "touchpointId";
        private const string ApimUrlHeaderParameterKey = "apimurl";

        private const string ApimUrlHeaderParameterValue = "http://localhost:7071/";
        private const string TouchpointIdHeaderParamValue = "9000000000";
        private const string ValidCustomerId = "7acfc365-dfa0-6f84-46f3-eb767420aaaa";

        private Mock<IDigitalIdentityServiceBusClient> _mockDigitalIdentityServiceBusClient;
        private Mock<IDocumentDBProvider> _mockDocumentDbProvider;
        private Mock<ILogger<PatchDigitalIdentityByCustomerIdHttpTrigger>> _logger;
        private Mock<IDynamicHelper> _dynamicHelper;

        private IGetDigitalIdentityHttpTriggerService _getDigitalIdentityByCustomerIdHttpTriggerService;
        private IDigitalIdentityService _patchDigitalIdentityHttpTriggerService;
        private IHttpRequestHelper _httpRequestHelper;
        private IValidate _validate;
        private IMapper _mapper;

        private PatchDigitalIdentityByCustomerIdHttpTrigger _function;

        [SetUp]
        public void Setup()
        {
            // Mocks
            _mockDigitalIdentityServiceBusClient = new Mock<IDigitalIdentityServiceBusClient>();
            _mockDocumentDbProvider = new Mock<IDocumentDBProvider>();
            _logger = new Mock<ILogger<PatchDigitalIdentityByCustomerIdHttpTrigger>>();
            _dynamicHelper = new Mock<IDynamicHelper>();

            // Below is a fudge as we cannot return ResourceResponse<Document> out of _mockDocumentDbProvider
            _validate = new Validate(_mockDocumentDbProvider.Object);
            _httpRequestHelper = new HttpRequestHelper();
            _getDigitalIdentityByCustomerIdHttpTriggerService = new GetDigitalIdentityHttpTriggerService(_mockDocumentDbProvider.Object);
            _patchDigitalIdentityHttpTriggerService =
                new DigitalIdentityService(_mockDocumentDbProvider.Object,
                    _mockDigitalIdentityServiceBusClient.Object);
            _mapper = new Mapper(new MapperConfiguration(item => item.AddProfile<MappingProfile>()));

            _function = new PatchDigitalIdentityByCustomerIdHttpTrigger(
                _patchDigitalIdentityHttpTriggerService,
                _getDigitalIdentityByCustomerIdHttpTriggerService,
                _httpRequestHelper,
                _validate,
                _logger.Object,
                _mapper,
                _dynamicHelper.Object
                );
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenValidPatchRequest_ThenResourceIsUpdated()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            var responseHttpBody = new Models.DigitalIdentity()
            {
                IdentityID = Guid.Parse(ValidCustomerId),
                CustomerId = httpRequestBody.CustomerId,
                IdentityStoreId = httpRequestBody.IdentityStoreID,
                LastModifiedDate = DateTime.UtcNow,
                LegacyIdentity = httpRequestBody.LegacyIdentity,
                id_token = httpRequestBody.id_token,
                LastModifiedTouchpointId = TouchpointIdHeaderParamValue,
                DateOfClosure = null,
            };

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.UpdateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                                                                        .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.GetCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Customer()));


            // Act
            var response = await RunFunction(ValidCustomerId, httpRequest);
            var responseResult = response as JsonResult;

            // Assert
            Assert.That(response, Is.InstanceOf<JsonResult>());
            Assert.That(responseResult.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenTouchPointIdMissing_ThenReturnBadRequest()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            httpRequest.Headers.Remove(TouchpointIdHeaderParamKey);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidCustomerId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenApimUrlMissing_ThenReturnBadRequest()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            httpRequest.Headers.Remove(ApimUrlHeaderParameterKey);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidCustomerId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityIdNotGuid_ThenReturnBadRequest()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction("invalidIdentityId", httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenRequestBodyMissing_ThenReturnUnprocessableEntity()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest(null);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidCustomerId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenCustomerDoesNotExists_ThenReturnUnprocessableEntity()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            Models.DigitalIdentity responseHttpBody = null;

            _mockDocumentDbProvider.Setup(m => m.GetIdentityForCustomerAsync(httpRequestBody.CustomerId))
                                    .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                    .Returns(Task.FromResult<bool>(false));

            // Act
            var result = await RunFunction(ValidCustomerId, httpRequest);
            var resultResponse = result as ObjectResult;

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
            Assert.That(resultResponse.Value.ToString(), Is.Not.Empty);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityDoesNotExists_ThenReturnNoContentEntity()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult<Models.DigitalIdentity>(null));
            _mockDocumentDbProvider.Setup(m => m.GetCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Customer()));

            // Act
            var result = await RunFunction(ValidCustomerId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityIsTerminated_ThenReturnNoContentEntity()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            var terminateDigitalIdentity = new Models.DigitalIdentity
            {
                IdentityID = Guid.Parse(ValidCustomerId),
                CustomerId = httpRequestBody.CustomerId,
                IdentityStoreId = httpRequestBody.IdentityStoreID,
                LastModifiedDate = DateTime.UtcNow,
                LegacyIdentity = httpRequestBody.LegacyIdentity,
                id_token = httpRequestBody.id_token,
                LastModifiedTouchpointId = TouchpointIdHeaderParamValue,
                DateOfClosure = DateTime.UtcNow.AddDays(-1), // Ensure resource is terminated
            };

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult(terminateDigitalIdentity));

            // Act
            var result = await RunFunction(ValidCustomerId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityResult>());
        }

        private async Task<IActionResult> RunFunction(string customerId, HttpRequest request)
        {
            return await _function.RunAsync(request, customerId).ConfigureAwait(false);
        }

        private static Stream GenerateStreamFromJson(string requestBody)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(requestBody);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private static HttpRequest GenerateDefaultHttpRequest(DigitalIdentityPatch requestBody)
        {
            var defaultRequest = new DefaultHttpContext().Request;

            defaultRequest.Headers.Append(TouchpointIdHeaderParamKey, TouchpointIdHeaderParamValue);
            defaultRequest.Headers.Append(ApimUrlHeaderParameterKey, ApimUrlHeaderParameterValue);
            defaultRequest.Body = GenerateStreamFromJson(JsonConvert.SerializeObject(requestBody));

            return defaultRequest;
        }

        private static DigitalIdentityPatch GenerateDefaultPatchRequestBody()
        {
            // Build request
            return new DigitalIdentityPatch
            {
                CustomerId = Guid.NewGuid(),
                IdentityStoreID = Guid.NewGuid(),
                //LastLoggedInDateTime = DateTime.UtcNow,
                LegacyIdentity = Guid.NewGuid().ToString(),
                id_token = "token",
            };
        }
    }
}
