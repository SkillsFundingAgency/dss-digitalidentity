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
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.UnitTests
{
    [TestFixture]
    public class PatchDigitalIdentityByIdentityIdHttpTriggerTests
    {
        private const string TouchpointIdHeaderParamKey = "touchpointId";
        private const string ApimUrlHeaderParameterKey = "apimurl";
        private const string ApimUrlHeaderParameterValue = "http://localhost:7071/";
        private const string TouchpointIdHeaderParamValue = "9000000000";
        private const string ValidIdentityId = "7acfc365-dfa0-6f84-46f3-eb767420aaaa";

        private Mock<IDigitalIdentityServiceBusClient> _mockDigitalIdentityServiceBusClient;
        private Mock<IDocumentDBProvider> _mockDocumentDbProvider;
        private Mock<ILogger<PatchDigitalIdentityByIdentityIdHttpTrigger>> _mockLog;
        private Mock<IDynamicHelper> _dynamicHelper;

        private IDigitalIdentityService _patchDigitalIdentityHttpTriggerService;
        private IGetDigitalIdentityHttpTriggerService _getDigitalIdentityHttpTriggerService;
        private IHttpRequestHelper _httpRequestHelper;
        private IValidate _validate;
        private IMapper _mapper;


        private PatchDigitalIdentityByIdentityIdHttpTrigger _function;

        [SetUp]
        public void Setup()
        {
            // Mocks
            _mockDigitalIdentityServiceBusClient = new Mock<IDigitalIdentityServiceBusClient>();
            _mockDocumentDbProvider = new Mock<IDocumentDBProvider>();
            _mockLog = new Mock<ILogger<PatchDigitalIdentityByIdentityIdHttpTrigger>>();
            _dynamicHelper = new Mock<IDynamicHelper>();

            // Below is a fudge as we cannot return ResourceResponse<Document> out of _mockDocumentDbProvider
            _patchDigitalIdentityHttpTriggerService = new DigitalIdentityService(_mockDocumentDbProvider.Object, _mockDigitalIdentityServiceBusClient.Object);
            _getDigitalIdentityHttpTriggerService = new GetDigitalIdentityHttpTriggerService(_mockDocumentDbProvider.Object);
            _httpRequestHelper = new HttpRequestHelper();
            _validate = new Validate(_mockDocumentDbProvider.Object);
            _mapper = new Mapper(new MapperConfiguration(item => item.AddProfile<MappingProfile>()));

            _function = new PatchDigitalIdentityByIdentityIdHttpTrigger(
                _patchDigitalIdentityHttpTriggerService,
                _getDigitalIdentityHttpTriggerService,
                _httpRequestHelper,
                _validate,
                _mockLog.Object,
                _mapper,
                _dynamicHelper.Object
            );

        }

        //TODO: Fix broken unit test
        /*[Test]
        public async Task GivenIdentityResourceExists_WhenValidPatchRequest_ThenResourceIsUpdated()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            var responsHttpBody = new Models.DigitalIdentity()
            {
                IdentityID = Guid.Parse(validIdentityId),
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
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult<Models.DigitalIdentity>(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.UpdateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                                                                        .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.GetCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Customer()));

            // Act
            var result = await RunFunction(validIdentityId, httpRequest);
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
                                                                        .Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidIdentityId, httpRequest);

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
            var result = await RunFunction(ValidIdentityId, httpRequest);

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
            var result = await RunFunction(ValidIdentityId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenCustomerDoesNotExists_ThenReturnUpdatedEntity()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            var responseHttpBody = new Models.DigitalIdentity
            {
                IdentityID = Guid.Parse(ValidIdentityId),
                CustomerId = httpRequestBody.CustomerId,
                IdentityStoreId = httpRequestBody.IdentityStoreID,
                LastModifiedDate = DateTime.UtcNow,
                LegacyIdentity = httpRequestBody.LegacyIdentity,
                id_token = httpRequestBody.id_token,
                LastModifiedTouchpointId = TouchpointIdHeaderParamValue,
                DateOfClosure = null,
            };

            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.UpdateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                    .Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(ValidIdentityId, httpRequest);
            var resultResponse = result as ObjectResult;

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
            Assert.That(resultResponse.Value.ToString(), Is.Not.Empty);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityDoesNotExists_ThenReturnUnprocessableEntity()
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
            var result = await RunFunction(ValidIdentityId, httpRequest);

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
                IdentityID = Guid.Parse(ValidIdentityId),
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
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(terminateDigitalIdentity));

            // Act
            var result = await RunFunction(ValidIdentityId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
        }

        private async Task<IActionResult> RunFunction(string identityId, HttpRequest request)
        {
            return await _function.RunAsync(request, identityId).ConfigureAwait(false);
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
            return new DigitalIdentityPatch()
            {
                CustomerId = Guid.NewGuid(),
                IdentityStoreID = Guid.NewGuid(),
                LastLoggedInDateTime = DateTime.UtcNow,
                LegacyIdentity = Guid.NewGuid().ToString(),
                id_token = "token",
            };
        }
    }
}
