using AutoMapper;
using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.DigitalIdentity.Cosmos.Helper;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
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

namespace NCS.DSS.DigitalIdentity.UnitTests
{
    [TestFixture]
    public class PostDigitalIdentityHttpTriggerTests
    {
        private const string TouchpointIdHeaderParamKey = "touchpointId";
        private const string ApimUrlHeaderParameterKey = "apimurl";
        private const string SubcontractorIdHeaderParamKey = "subcontractorId";

        private string ApimUrlHeaderParameterValue = "http://localhost:7071/";
        private string TouchpointIdHeaderParamValue = "9000000000";
        private string SubcontractorIdHeaderParamValue = "9999999999";

        private Mock<ILogger> _mockLog;
        private Mock<IDocumentDBProvider> _mockDocumentDbProvider;
        private Mock<ILoggerHelper> _loggerHelper;
        private Mock<IDigitalIdentityServiceBusClient> _mockDigitalIdentityServiceBusClient;

        private IDigitalIdentityService _postDigitalIdentityHttpTriggerService;
        private IResourceHelper _resourceHelper;
        private IHttpRequestHelper _httpRequestHelper;
        private IHttpResponseMessageHelper _httpResponseMessageHelper;
        private IJsonHelper _jsonHelper;
        private IValidate _validate;
        private IMapper _mapper;

        [SetUp]
        public void Setup()
        {
            // Mocks
            _mockLog = new Mock<ILogger>();
            _mockDocumentDbProvider = new Mock<IDocumentDBProvider>();
            _mockDigitalIdentityServiceBusClient = new Mock<IDigitalIdentityServiceBusClient>();
            _loggerHelper = new Mock<ILoggerHelper>();

            // Below is a fudge as we cannot return ResourceResponse<Document> out of _mockDocumentDbProvider
            _postDigitalIdentityHttpTriggerService = new DigitalIdentityService(_mockDocumentDbProvider.Object, _mockDigitalIdentityServiceBusClient.Object);

            _resourceHelper = new ResourceHelper(_mockDocumentDbProvider.Object);
            _validate = new Validate(_mockDocumentDbProvider.Object);
            _httpRequestHelper = new HttpRequestHelper();
            _httpResponseMessageHelper = new HttpResponseMessageHelper();
            _jsonHelper = new JsonHelper();
            _mapper = new Mapper(new MapperConfiguration(item => item.AddProfile<MappingProfile>()));

        }

        [Test]
        public async Task GivenValidPostRequest_WhenResourceCreated_ThenResourceIsReturned()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            var responsHttpBody = GenerateResponseFromRequest(httpRequestBody);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult<bool>(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.CreateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.GetCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Customer() { GivenName="test", FamilyName="test" }));
            _mockDocumentDbProvider.Setup(m => m.GetCustomerContact(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Contact() { EmailAddress = "email@email.com" }));

            // Act
            var result = await RunFunction(httpRequest);
            var actualResult =
                JsonConvert.DeserializeObject<Models.DigitalIdentity>(await result.Content.ReadAsStringAsync());

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
            Assert.AreEqual(actualResult.CustomerId, httpRequestBody.CustomerId);
            Assert.AreEqual(actualResult.id_token, httpRequestBody.id_token);
            Assert.AreEqual(actualResult.LastLoggedInDateTime, httpRequestBody.LastLoggedInDateTime);
        }

        [Test]
        public async Task GivenValidPostRequest_WhenTouchpointIdMissing_ThenReturnBadRequest()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            httpRequest.Headers.Remove(TouchpointIdHeaderParamKey);

            // Act
            var result = await RunFunction(httpRequest);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            _loggerHelper.Verify(l => l.LogInformationMessage(_mockLog.Object, It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task GivenValidPostRequest_WhenApimUrlMissing_ThenReturnBadRequest()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            httpRequest.Headers.Remove(ApimUrlHeaderParameterKey);

            // Act
            var result = await RunFunction(httpRequest);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            _loggerHelper.Verify(l => l.LogInformationMessage(_mockLog.Object, It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task GivenValidPostRequest_WhenRequestBodyMissing_ThenReturnBadRequest()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest(null);

            // Act
            var result = await RunFunction(httpRequest);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, result.StatusCode);
            _loggerHelper.Verify(l => l.LogInformationMessage(_mockLog.Object, It.IsAny<Guid>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task GivenValidPostRequest_WhenCustomerDoesNotExists_ThenReturnBadRequest()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult<bool>(false));

            // Act
            var result = await RunFunction(httpRequest);
            var contentBody = await result.Content.ReadAsStringAsync();

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, result.StatusCode);
            Assert.IsNotEmpty(contentBody);
        }

        [Test]
        public async Task GivenValidPostRequest_WhenResourceCreationFailed_ThenReturnInternalServerError()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            Models.DigitalIdentity responsHttpBody = null;

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult<bool>(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.CreateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.GetCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Customer() { GivenName = "test", FamilyName = "test" }));
            _mockDocumentDbProvider.Setup(m => m.GetCustomerContact(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Contact() { EmailAddress = "email@email.com" }));

            // Act
            var result = await RunFunction(httpRequest);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        }


        [Test]
        public async Task GivenValidPostRequest_WhenReadOnlyCustomer_ThenReturnError()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            Models.DigitalIdentity responsHttpBody = null;

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult<bool>(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.CreateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.GetCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Customer { id = Guid.NewGuid(), DateOfTermination=DateTime.Now.AddDays(-1) }));

            // Act
            var result = await RunFunction(httpRequest);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        }

        [Test]
        public async Task GivenValidPostRequest_WhenCustomerAlreadyHasDigitalIdentity_ThenReturnError()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            Models.DigitalIdentity responsHttpBody = null;

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult<bool>(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.CreateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                .Returns(Task.FromResult(responsHttpBody));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(responsHttpBody));

            // Act
            var result = await RunFunction(httpRequest);

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

        private async Task<HttpResponseMessage> RunFunction(HttpRequest request)
        {
            return await PostDigitalIdentityHttpTrigger.Function.PostDigitalIdentityHttpTrigger.RunAsync(
                request,
                _mockLog.Object,
                _postDigitalIdentityHttpTriggerService,
                _loggerHelper.Object,
                _httpRequestHelper,
                _httpResponseMessageHelper,
                _jsonHelper,
                _validate,
                _mockDocumentDbProvider.Object,
                _mockDigitalIdentityServiceBusClient.Object,
                _mapper
            ).ConfigureAwait(false);
        }

        private DefaultHttpRequest GenerateDefaultHttpRequest(Models.DigitalIdentity requestBody)
        {
            var defaultRequest = new DefaultHttpRequest(new DefaultHttpContext());

            defaultRequest.Headers.Add(TouchpointIdHeaderParamKey, TouchpointIdHeaderParamValue);
            defaultRequest.Headers.Add(ApimUrlHeaderParameterKey, ApimUrlHeaderParameterValue);
            defaultRequest.Headers.Add(SubcontractorIdHeaderParamKey, SubcontractorIdHeaderParamValue);
            defaultRequest.Body = GenerateStreamFromJson(JsonConvert.SerializeObject(requestBody));

            return defaultRequest;
        }

        private Models.DigitalIdentity GenerateDefaultPostRequestBody()
        {
            // Build request
            return new Models.DigitalIdentity()
            {
                CustomerId = Guid.NewGuid(),
                IdentityStoreId = Guid.NewGuid(),
                LastLoggedInDateTime = DateTime.UtcNow,
                LegacyIdentity = Guid.NewGuid().ToString(),
                id_token = "token",
            };
        }

        private Models.DigitalIdentity GenerateResponseFromRequest(Models.DigitalIdentity httpRequestBody)
        {
            return new Models.DigitalIdentity()
            {
                IdentityID = Guid.NewGuid(),
                CustomerId = httpRequestBody.CustomerId,
                IdentityStoreId = httpRequestBody.IdentityStoreId,
                LastLoggedInDateTime = httpRequestBody.LastLoggedInDateTime,
                LastModifiedDate = DateTime.UtcNow,
                LegacyIdentity = httpRequestBody.LegacyIdentity,
                id_token = httpRequestBody.id_token,
                LastModifiedTouchpointId = TouchpointIdHeaderParamValue,
                DateOfClosure = null,
            };
        }

        #endregion
    }
}
