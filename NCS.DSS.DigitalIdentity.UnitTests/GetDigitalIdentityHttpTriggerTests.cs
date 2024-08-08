using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.UnitTests
{
    [TestFixture]
    public class GetDigitalIdentityHttpTriggerTests
    {
        private const string TouchpointIdHeaderParamKey = "touchpointId";
        private const string ApimUrlHeaderParameterKey = "apimurl";
        private const string ApimUrlHeaderParameterValue = "http://localhost:7071/";
        private const string TouchpointIdHeaderParamValue = "9000000000";
        private const string ValidIdentityId = "fb1cd730-720a-4f3a-b160-6ff8785c37f2";
        private const string InvalidIdentityId = "aabbcc";

        private Mock<IDocumentDBProvider> _mockDocumentDbProvider;
        private Mock<ILoggerHelper> _loggerHelper;
        private Mock<ILogger<GetDigitalIdentityHttpTrigger.Function.GetDigitalIdentityHttpTrigger>> _logger;

        private IGetDigitalIdentityHttpTriggerService _getDigitalIdentityHttpTriggerService;
        private IHttpRequestHelper _httpRequestHelper;

        private GetDigitalIdentityHttpTrigger.Function.GetDigitalIdentityHttpTrigger _function;

        [SetUp]
        public void Setup()
        {
            // Mocks
            _mockDocumentDbProvider = new Mock<IDocumentDBProvider>();
            _loggerHelper = new Mock<ILoggerHelper>();
            _logger = new Mock<ILogger<GetDigitalIdentityHttpTrigger.Function.GetDigitalIdentityHttpTrigger>>();

            _getDigitalIdentityHttpTriggerService = new GetDigitalIdentityHttpTriggerService(_mockDocumentDbProvider.Object);
            _httpRequestHelper = new HttpRequestHelper();

            _function = new GetDigitalIdentityHttpTrigger.Function.GetDigitalIdentityHttpTrigger(
                _getDigitalIdentityHttpTriggerService,
                _httpRequestHelper,
                _loggerHelper.Object,
                _logger.Object);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenValidGetRequest_ThenResourceIsReturned()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest();
            var httpResponse = GenerateDefaultHttpResponse();

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult(httpResponse));

            // Act
            var result = await RunFunction(ValidIdentityId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenTouchPointIdMissing_ThenBadRequest()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest();
            httpRequest.Headers.Remove(TouchpointIdHeaderParamKey);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidIdentityId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenCustomerIdIsNotGuid_ThenBadRequest()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest();

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(InvalidIdentityId, httpRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenCustomerDoesNotExists_ThenBadRequest()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest();

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(ValidIdentityId, httpRequest);
            var resultResponse = result as ObjectResult;

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            Assert.That(resultResponse.StatusCode, Is.EqualTo((int)HttpStatusCode.NoContent));
        }

        private async Task<IActionResult> RunFunction(string customerId, HttpRequest request)
        {
            return await _function.Run(request, customerId).ConfigureAwait(false);
        }

        private static Models.DigitalIdentity GenerateDefaultHttpResponse()
        {
            return new Models.DigitalIdentity()
            {
                IdentityID = Guid.NewGuid(),
                CustomerId = Guid.Parse(ValidIdentityId),
                IdentityStoreId = Guid.NewGuid(),
                LastModifiedDate = DateTime.UtcNow,
                LegacyIdentity = Guid.NewGuid().ToString(),
                id_token = "test_token",
                LastModifiedTouchpointId = TouchpointIdHeaderParamValue,
                DateOfClosure = null,
            };
        }

        private static HttpRequest GenerateDefaultHttpRequest()
        {
            var defaultRequest = new DefaultHttpContext().Request;

            defaultRequest.Headers.Add(TouchpointIdHeaderParamKey, TouchpointIdHeaderParamValue);
            defaultRequest.Headers.Add(ApimUrlHeaderParameterKey, ApimUrlHeaderParameterValue);

            return defaultRequest;
        }
    }
}
