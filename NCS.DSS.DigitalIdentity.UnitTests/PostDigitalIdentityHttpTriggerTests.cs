using AutoMapper;
using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
using System.Threading.Tasks;
using System.Web.Http;

namespace NCS.DSS.DigitalIdentity.UnitTests
{
    [TestFixture]
    public class PostDigitalIdentityHttpTriggerTests
    {
        private const string TouchpointIdHeaderParamKey = "touchpointId";
        private const string ApimUrlHeaderParameterKey = "apimurl";
        private const string ApimUrlHeaderParameterValue = "http://localhost:7071/";
        private const string TouchpointIdHeaderParamValue = "9000000000";

        private Mock<IDigitalIdentityServiceBusClient> _mockDigitalIdentityServiceBusClient;
        private Mock<IDocumentDBProvider> _mockDocumentDbProvider;
        private Mock<ILoggerHelper> _loggerHelper;
        private Mock<ILogger<PostDigitalIdentityHttpTrigger.Function.PostDigitalIdentityHttpTrigger>> _logger;
        private Mock<IDynamicHelper> _dynamicHelper;

        private IDigitalIdentityService _postDigitalIdentityHttpTriggerService;
        private IHttpRequestHelper _httpRequestHelper;
        private IValidate _validate;
        private IMapper _mapper;

        private PostDigitalIdentityHttpTrigger.Function.PostDigitalIdentityHttpTrigger _function;

        [SetUp]
        public void Setup()
        {
            // Mocks
            _mockDigitalIdentityServiceBusClient = new Mock<IDigitalIdentityServiceBusClient>();
            _mockDocumentDbProvider = new Mock<IDocumentDBProvider>();
            _loggerHelper = new Mock<ILoggerHelper>();
            _logger = new Mock<ILogger<PostDigitalIdentityHttpTrigger.Function.PostDigitalIdentityHttpTrigger>>();
            _dynamicHelper = new Mock<IDynamicHelper>();

            // Below is a fudge as we cannot return ResourceResponse<Document> out of _mockDocumentDbProvider
            _postDigitalIdentityHttpTriggerService = new DigitalIdentityService(_mockDocumentDbProvider.Object, _mockDigitalIdentityServiceBusClient.Object);
            _httpRequestHelper = new HttpRequestHelper();
            _validate = new Validate(_mockDocumentDbProvider.Object);
            _mapper = new Mapper(new MapperConfiguration(item => item.AddProfile<MappingProfile>()));

            _function = new PostDigitalIdentityHttpTrigger.Function.PostDigitalIdentityHttpTrigger(
                _postDigitalIdentityHttpTriggerService,
                _mockDigitalIdentityServiceBusClient.Object,
                _httpRequestHelper,
                _mockDocumentDbProvider.Object,
                _loggerHelper.Object,
                _logger.Object,
                _validate,
                _mapper,
                _dynamicHelper.Object);
        }

        [Test]
        public async Task GivenValidPostRequest_WhenResourceCreated_ThenResourceIsReturned()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            var responseHttpBody = GenerateResponseFromRequest(httpRequestBody);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.CreateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.GetCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Customer() { GivenName = "test", FamilyName = "test" }));
            _mockDocumentDbProvider.Setup(m => m.GetCustomerContact(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Contact() { EmailAddress = "email@email.com" }));

            // Act
            var result = await RunFunction(httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<CreatedResult>());
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
            Assert.That(result, Is.InstanceOf<BadRequestResult>());
            _loggerHelper.Verify(l => l.LogInformationMessage(_logger.Object, It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
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
            Assert.That(result, Is.InstanceOf<BadRequestResult>());
            _loggerHelper.Verify(l => l.LogInformationMessage(_logger.Object, It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task GivenValidPostRequest_WhenRequestBodyMissing_ThenReturnBadRequest()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest(null);

            // Act
            var result = await RunFunction(httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityResult>());
            _loggerHelper.Verify(l => l.LogInformationMessage(_logger.Object, It.IsAny<Guid>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task GivenValidPostRequest_WhenCustomerDoesNotExists_ThenReturnBadRequest()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(httpRequest);
            var resultResponse = result as ObjectResult;

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
            Assert.IsNotEmpty(resultResponse.Value.ToString());
        }

        [Test]
        public async Task GivenValidPostRequest_WhenResourceCreationFailed_ThenReturnInternalServerError()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            Models.DigitalIdentity responseHttpBody = null;

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.CreateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.GetCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Customer() { GivenName = "test", FamilyName = "test" }));
            _mockDocumentDbProvider.Setup(m => m.GetCustomerContact(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Contact() { EmailAddress = "email@email.com" }));

            // Act
            var result = await RunFunction(httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<InternalServerErrorResult>());
        }


        [Test]
        public async Task GivenValidPostRequest_WhenReadOnlyCustomer_ThenReturnError()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            Models.DigitalIdentity responseHttpBody = null;

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.CreateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.GetCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Customer { id = Guid.NewGuid(), DateOfTermination = DateTime.Now.AddDays(-1) }));

            // Act
            var result = await RunFunction(httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
        }

        [Test]
        public async Task GivenValidPostRequest_WhenCustomerAlreadyHasDigitalIdentity_ThenReturnError()
        {
            // Arrange
            var httpRequestBody = GenerateDefaultPostRequestBody();
            var httpRequest = GenerateDefaultHttpRequest(httpRequestBody);
            Models.DigitalIdentity responseHttpBody = null;

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.CreateIdentityAsync(It.IsAny<Models.DigitalIdentity>()))
                .Returns(Task.FromResult(responseHttpBody));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(responseHttpBody));

            // Act
            var result = await RunFunction(httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
        }

        private async Task<IActionResult> RunFunction(HttpRequest request)
        {
            return await _function.RunAsync(request).ConfigureAwait(false);
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

        private static HttpRequest GenerateDefaultHttpRequest(Models.DigitalIdentity requestBody)
        {
            var defaultRequest = new DefaultHttpContext().Request;

            defaultRequest.Headers.Add(TouchpointIdHeaderParamKey, TouchpointIdHeaderParamValue);
            defaultRequest.Headers.Add(ApimUrlHeaderParameterKey, ApimUrlHeaderParameterValue);
            defaultRequest.Body = GenerateStreamFromJson(JsonConvert.SerializeObject(requestBody));

            return defaultRequest;
        }

        private static Models.DigitalIdentity GenerateDefaultPostRequestBody()
        {
            // Build request
            return new Models.DigitalIdentity
            {
                CustomerId = Guid.NewGuid(),
                IdentityStoreId = Guid.NewGuid(),
                LegacyIdentity = Guid.NewGuid().ToString(),
                id_token = "token",
            };
        }

        private static Models.DigitalIdentity GenerateResponseFromRequest(Models.DigitalIdentity httpRequestBody)
        {
            return new Models.DigitalIdentity
            {
                IdentityID = Guid.NewGuid(),
                CustomerId = httpRequestBody.CustomerId,
                IdentityStoreId = httpRequestBody.IdentityStoreId,
                LastModifiedDate = DateTime.UtcNow,
                LegacyIdentity = httpRequestBody.LegacyIdentity,
                id_token = httpRequestBody.id_token,
                LastModifiedTouchpointId = TouchpointIdHeaderParamValue,
                DateOfClosure = null,
            };
        }
    }
}
