using AutoMapper;
using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.DigitalIdentity.Cosmos.Helper;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.DTO;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.Interfaces;
using NCS.DSS.DigitalIdentity.Mappings;
using NCS.DSS.DigitalIdentity.Models;
using NCS.DSS.DigitalIdentity.Services;
using NCS.DSS.DigitalIdentity.Validation;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NCS.DSS.DigitalIdentity.PatchDigitalIdentityHttpTrigger.Function;

namespace NCS.DSS.DigitalIdentity.UnitTests
{
    [TestFixture]
    public class PatchDigitalIdentityByCustomerIdHttpTriggerTests
    {
        private const string TouchpointIdHeaderParamKey = "touchpointId";
        private const string ApimUrlHeaderParameterKey = "apimurl";

        private string ApimUrlHeaderParameterValue = "http://localhost:7071/";
        private string TouchpointIdHeaderParamValue = "9000000000";
        private string validCustomerId = "7acfc365-dfa0-6f84-46f3-eb767420aaaa";

        private Mock<ILogger> _mockLog;
        private Mock<IDocumentDBProvider> _mockDocumentDbProvider;
        private Mock<ILoggerHelper> _loggerHelper;
        private Mock<IDigitalIdentityServiceBusClient> _mockDigitalIdentityServiceBusClient;
        private Mock<IDigitalIdentityService> _mockDigitalIdentityService;
        private Mock<IDynamicHelper> _dynamicHelper;
        private Mock<ILogger<PatchDigitalIdentityByCustomerIdHttpTrigger>> _logger;

        private IDigitalIdentityService _patchDigitalIdentityHttpTriggerService;
        private IResourceHelper _resourceHelper;
        private IGetDigitalIdentityHttpTriggerService _getDigitalIdentityByCustomerIdHttpTriggerService;
        private IHttpRequestHelper _httpRequestHelper;
        private IHttpResponseMessageHelper _httpResponseMessageHelper;
        private IJsonHelper _jsonHelper;
        private IValidate _validate;
        private IMapper _mapper;
        private PatchDigitalIdentityByCustomerIdHttpTrigger _function;

        [SetUp]
        public void Setup()
        {
            // Mocks
            _mockLog = new Mock<ILogger>();
            _mockDocumentDbProvider = new Mock<IDocumentDBProvider>();
            _mockDigitalIdentityServiceBusClient = new Mock<IDigitalIdentityServiceBusClient>();
            _loggerHelper = new Mock<ILoggerHelper>();
            _mockDigitalIdentityService = new Mock<IDigitalIdentityService>();
            _dynamicHelper = new Mock<IDynamicHelper>();
            _logger = new Mock<ILogger<PatchDigitalIdentityByCustomerIdHttpTrigger>>();

            // Below is a fudge as we cannot return ResourceResponse<Document> out of _mockDocumentDbProvider

            _resourceHelper = new ResourceHelper(_mockDocumentDbProvider.Object);
            _validate = new Validate(_mockDocumentDbProvider.Object);
            _httpRequestHelper = new HttpRequestHelper();
            _httpResponseMessageHelper = new HttpResponseMessageHelper();
            _jsonHelper = new JsonHelper();
            _getDigitalIdentityByCustomerIdHttpTriggerService = new GetDigitalIdentityHttpTriggerService(_mockDocumentDbProvider.Object);
            _patchDigitalIdentityHttpTriggerService =
                new DigitalIdentityService(_mockDocumentDbProvider.Object,
                    _mockDigitalIdentityServiceBusClient.Object);
            _mapper = new Mapper(new MapperConfiguration(item => item.AddProfile<MappingProfile>()));

            _function = new PatchDigitalIdentityByCustomerIdHttpTrigger(
                _mockDigitalIdentityService.Object,
                _getDigitalIdentityByCustomerIdHttpTriggerService,
                _httpRequestHelper,
                _validate,
                _loggerHelper.Object,
                _logger.Object,
                _mapper,
                _dynamicHelper.Object
                );
        }

        /*[Test]
        public async Task GivenIdentityResourceExists_WhenValidPatchRequest_ThenResourceIsUpdated()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            var responsHttpBody = new Models.DigitalIdentity()
            {
                IdentityID = Guid.Parse(validCustomerId),
                CustomerId = httpRequestBody.CustomerId,
                IdentityStoreId = httpRequestBody.IdentityStoreID,
                LastModifiedDate = DateTime.UtcNow,
                LegacyIdentity = httpRequestBody.LegacyIdentity,
                id_token = httpRequestBody.id_token,
                LastModifiedTouchpointId = TouchpointIdHeaderParamValue,
                DateOfClosure = null,
            };

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult<bool>(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult<Models.DigitalIdentity>(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.UpdateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                                                                        .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.GetCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Customer()));


            // Act
            var result = await RunFunction(validCustomerId, httpRequest);
            //var actualResult = JsonConvert.DeserializeObject<Models.DigitalIdentity>(await result.Content.ReadAsStringAsync());

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            *//*Assert.AreEqual(actualResult.CustomerId, httpRequestBody.CustomerId);
            Assert.AreEqual(actualResult.id_token, httpRequestBody.id_token);*//*
            //Assert.AreEqual(actualResult.LastLoggedInDateTime, httpRequestBody.LastLoggedInDateTime);
        }*/

        [Test]
        public async Task GivenIdentityResourceExists_WhenTouchPointIdMissing_ThenReturnBadRequest()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            httpRequest.Headers.Remove(TouchpointIdHeaderParamKey);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult<bool>(true));

            // Act
            var result = await RunFunction(validCustomerId, httpRequest);

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
                .Returns(Task.FromResult<bool>(true));

            // Act
            var result = await RunFunction(validCustomerId, httpRequest);

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
                .Returns(Task.FromResult<bool>(true));

            // Act
            var result = await RunFunction("invalidIdentityId", httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenRequestBodyMissing_ThenReturnUnprocessibleEntity()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(null);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult<bool>(true));

            // Act
            var result = await RunFunction(validCustomerId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenCustomerDoesNotExists_ThenReturnUnprocessibleEntity()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            Models.DigitalIdentity responsHttpBody = null;

            _mockDocumentDbProvider.Setup(m => m.GetIdentityForCustomerAsync(httpRequestBody.CustomerId))
                                    .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                    .Returns(Task.FromResult<bool>(false));

            // Act
            var result = await RunFunction(validCustomerId, httpRequest);
            //var contentBody = await result.Content.ReadAsStringAsync();

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
            //Assert.IsNotEmpty(contentBody);
        }

        /*[Test]
        public async Task GivenIdentityResourceExists_WhenIdentityDoesNotExists_ThenReturnNoContentEntity()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult<bool>(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult<Models.DigitalIdentity>(null));
            _mockDocumentDbProvider.Setup(m => m.GetCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Customer()));

            // Act
            var result = await RunFunction(validCustomerId, httpRequest);
            //var contentBody = await result.Content.ReadAsStringAsync();

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
            //Assert.IsNotEmpty(contentBody);
        }*/

        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityIsTerminated_ThenReturnNoContentEntity()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            var terminateDigitalIdentity = new Models.DigitalIdentity()
            {
                IdentityID = Guid.Parse(validCustomerId),
                CustomerId = httpRequestBody.CustomerId,
                IdentityStoreId = httpRequestBody.IdentityStoreID,
                LastModifiedDate = DateTime.UtcNow,
                LegacyIdentity = httpRequestBody.LegacyIdentity,
                id_token = httpRequestBody.id_token,
                LastModifiedTouchpointId = TouchpointIdHeaderParamValue,
                DateOfClosure = DateTime.UtcNow.AddDays(-1), // Ensure resource is terminated
            };

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult<bool>(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult<Models.DigitalIdentity>(terminateDigitalIdentity));

            // Act
            var result = await RunFunction(validCustomerId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
        }

        #region Helpers

        private Stream GenerateStreamFromJson(string requestBody)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(requestBody);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private async Task<IActionResult> RunFunction(string customerId, HttpRequest request)
        {
            return await _function.RunAsync(request, customerId).ConfigureAwait(false);
        }

        private HttpRequest GenerateDefaultHttpRequest(DigitalIdentityPatch requestBody)
        {
            var defaultRequest = new DefaultHttpContext().Request;

            defaultRequest.Headers.Add(TouchpointIdHeaderParamKey, TouchpointIdHeaderParamValue);
            defaultRequest.Headers.Add(ApimUrlHeaderParameterKey, ApimUrlHeaderParameterValue);
            defaultRequest.Body = GenerateStreamFromJson(JsonConvert.SerializeObject(requestBody));

            return defaultRequest;
        }

        private DigitalIdentityPatch GenerateDefaultPatchRequestBody()
        {
            // Build request
            return new DigitalIdentityPatch()
            {
                CustomerId = Guid.NewGuid(),
                IdentityStoreID = Guid.NewGuid(),
                //LastLoggedInDateTime = DateTime.UtcNow,
                LegacyIdentity = Guid.NewGuid().ToString(),
                id_token = "token",
            };
        }

        #endregion
    }
}
