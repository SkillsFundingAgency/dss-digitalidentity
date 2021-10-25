using AutoMapper;
using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Configuration;
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
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.UnitTests
{
    [TestFixture]
    public class PatchDigitalIdentityByIdentityIdHttpTriggerTests
    {
        private const string TouchpointIdHeaderParamKey = "touchpointId";
        private const string ApimUrlHeaderParameterKey = "apimurl";

        private string ApimUrlHeaderParameterValue = "http://localhost:7071/";
        private string TouchpointIdHeaderParamValue = "1000000000";
        private string NonFrontendTouchpointId = "9000000000";
        private string validIdentityId = "7acfc365-dfa0-6f84-46f3-eb767420aaaa";

        private Mock<ILogger> _mockLog;
        private Mock<IDocumentDBProvider> _mockDocumentDbProvider;
        private Mock<ILoggerHelper> _loggerHelper;
        private Mock<IDigitalIdentityServiceBusClient> _mockDigitalIdentityServiceBusClient;


        private IDigitalIdentityService _patchDigitalIdentityHttpTriggerService;
        private IResourceHelper _resourceHelper;
        private IGetDigitalIdentityHttpTriggerService _getDigitalIdentityHttpTriggerService;
        private IHttpRequestHelper _httpRequestHelper;
        private IHttpResponseMessageHelper _httpResponseMessageHelper;
        private IJsonHelper _jsonHelper;
        private IValidate _validate;
        private IMapper _mapper;
        private IConfiguration _config;

        [SetUp]
        public void Setup()
        {
            // Mocks
            _mockLog = new Mock<ILogger>();
            _mockDocumentDbProvider = new Mock<IDocumentDBProvider>();
            _mockDigitalIdentityServiceBusClient = new Mock<IDigitalIdentityServiceBusClient>();
            _loggerHelper = new Mock<ILoggerHelper>();

            // Below is a fudge as we cannot return ResourceResponse<Document> out of _mockDocumentDbProvider
            _patchDigitalIdentityHttpTriggerService = new DigitalIdentityService(_mockDocumentDbProvider.Object, _mockDigitalIdentityServiceBusClient.Object);

            _resourceHelper = new ResourceHelper(_mockDocumentDbProvider.Object);
            _validate = new Validate(_mockDocumentDbProvider.Object);
            _httpRequestHelper = new HttpRequestHelper();
            _httpResponseMessageHelper = new HttpResponseMessageHelper();
            _jsonHelper = new JsonHelper();
            _getDigitalIdentityHttpTriggerService = new GetDigitalIdentityHttpTriggerService(_mockDocumentDbProvider.Object);
            _mapper = new Mapper(new MapperConfiguration(item => item.AddProfile<MappingProfile>()));
            var json = "{ \"TouchPointsPermittedToUpdateLastLoggedIn\": \"0000000997,1000000000\" }";
            _config = new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(json))).Build();

        }

        [Test]
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
                LastLoggedInDateTime = httpRequestBody.LastLoggedInDateTime,
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
            var actualResult = JsonConvert.DeserializeObject<Models.DigitalIdentity>(await result.Content.ReadAsStringAsync());

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual(actualResult.CustomerId, httpRequestBody.CustomerId);
            Assert.AreEqual(actualResult.id_token, httpRequestBody.id_token);
            Assert.AreEqual(actualResult.LastLoggedInDateTime, httpRequestBody.LastLoggedInDateTime);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenTouchPointIdMissing_ThenReturnBadRequest()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            httpRequest.Headers.Remove(TouchpointIdHeaderParamKey);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult<bool>(true));

            var result = await RunFunction(validIdentityId, httpRequest);
            // Act

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            _loggerHelper.Verify(l => l.LogInformationMessage(_mockLog.Object, It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        public async Task GivenIdentityResourceExists_NotPermittedToUpdateLastLoggedInDate_ThenResourceIsNoUpdated()
        {
            // Arrange
            var json = "{ \"Values\": { \"TouchPointsPermittedToUpdateLastLoggedIn\": \"2222222222,1111111111\" }}";
            _config = new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(json))).Build();
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            var responsHttpBody = new Models.DigitalIdentity()
            {
                IdentityID = Guid.Parse(validIdentityId),
                CustomerId = httpRequestBody.CustomerId,
                IdentityStoreId = httpRequestBody.IdentityStoreID,
                LastLoggedInDateTime = httpRequestBody.LastLoggedInDateTime,
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
            var result = await RunFunction(validIdentityId, httpRequest);

            // Assert
            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, result.StatusCode);
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
            var result = await RunFunction(validIdentityId, httpRequest);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            _loggerHelper.Verify(l => l.LogInformationMessage(_mockLog.Object, It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
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
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            _loggerHelper.Verify(l => l.LogInformationMessage(_mockLog.Object, It.IsAny<Guid>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenRequestBodyMissing_ThenReturnUnprocessibleEntity()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest(null);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult<bool>(true));

            // Act
            var result = await RunFunction(validIdentityId, httpRequest);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenCustomerDoesNotExists_ThenReturnUpdatedEntity()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            var responsHttpBody = new Models.DigitalIdentity()
            {
                IdentityID = Guid.Parse(validIdentityId),
                CustomerId = httpRequestBody.CustomerId,
                IdentityStoreId = httpRequestBody.IdentityStoreID,
                LastLoggedInDateTime = httpRequestBody.LastLoggedInDateTime,
                LastModifiedDate = DateTime.UtcNow,
                LegacyIdentity = httpRequestBody.LegacyIdentity,
                id_token = httpRequestBody.id_token,
                LastModifiedTouchpointId = TouchpointIdHeaderParamValue,
                DateOfClosure = null,
            };

            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult<Models.DigitalIdentity>(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.UpdateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                    .Returns(Task.FromResult<bool>(false));

            // Act
            var result = await RunFunction(validIdentityId, httpRequest);
            var contentBody = await result.Content.ReadAsStringAsync();

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, result.StatusCode);
            Assert.IsNotEmpty(contentBody);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityDoesNotExists_ThenReturnUnprocessibleEntity()
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
            var result = await RunFunction(validIdentityId, httpRequest);
            var contentBody = await result.Content.ReadAsStringAsync();

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
            Assert.IsNotEmpty(contentBody);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityIsTerminated_ThenReturnNoContentEntity()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            var terminateDigitalIdentity = new Models.DigitalIdentity()
            {
                IdentityID = Guid.Parse(validIdentityId),
                CustomerId = httpRequestBody.CustomerId,
                IdentityStoreId = httpRequestBody.IdentityStoreID,
                LastLoggedInDateTime = httpRequestBody.LastLoggedInDateTime,
                LastModifiedDate = DateTime.UtcNow,
                LegacyIdentity = httpRequestBody.LegacyIdentity,
                id_token = httpRequestBody.id_token,
                LastModifiedTouchpointId = TouchpointIdHeaderParamValue,
                DateOfClosure = DateTime.UtcNow.AddDays(-1), // Ensure resource is terminated
            };

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult<bool>(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult<Models.DigitalIdentity>(terminateDigitalIdentity));

            // Act
            var result = await RunFunction(validIdentityId, httpRequest);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        }

        [Test]
        public async Task GivenValidPatchRequest_WhenNonFrontendTouchpointUpdatesLastLoggedInDate_ThenReturnError()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPatchRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            httpRequest.Headers.Remove(TouchpointIdHeaderParamKey);
            httpRequest.Headers.Add(TouchpointIdHeaderParamKey, NonFrontendTouchpointId);
            var responsHttpBody = new Models.DigitalIdentity()
            {
                IdentityID = Guid.Parse(validIdentityId),
                CustomerId = httpRequestBody.CustomerId,
                IdentityStoreId = httpRequestBody.IdentityStoreID,
                LastLoggedInDateTime = httpRequestBody.LastLoggedInDateTime,
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

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, result.StatusCode);
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

        private async Task<HttpResponseMessage> RunFunction(string identityId, HttpRequest request)
        {
            return await PatchDigitalIdentityHttpTrigger.Function.PatchDigitalIdentityByIdentityIdHttpTrigger.RunAsync(
                request,
                _mockLog.Object,
                identityId,
                _patchDigitalIdentityHttpTriggerService,
                _getDigitalIdentityHttpTriggerService,
                _loggerHelper.Object,
                _httpRequestHelper,
                _httpResponseMessageHelper,
                _jsonHelper,
                _validate,
                _mapper,
                _config
            ).ConfigureAwait(false);
        }

        private DefaultHttpRequest GenerateDefaultHttpRequest(DigitalIdentityPatch requestBody)
        {
            var defaultRequest = new DefaultHttpRequest(new DefaultHttpContext());

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
                LastLoggedInDateTime = DateTime.UtcNow,
                LegacyIdentity = Guid.NewGuid().ToString(),
                id_token = "token",
            };
        }

        #endregion
    }
}
