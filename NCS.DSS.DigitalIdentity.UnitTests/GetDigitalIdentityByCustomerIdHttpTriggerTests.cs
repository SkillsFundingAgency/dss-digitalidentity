using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Function;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.UnitTests
{
    [TestFixture]
    public class GetDigitalIdentityByCustomerIdHttpTriggerTests
    {
        private const string TouchpointIdHeaderParamKey = "touchpointId";
        private const string ApimUrlHeaderParameterKey = "apimurl";
        private const string ApimUrlHeaderParameterValue = "http://localhost:7071/";
        private const string TouchpointIdHeaderParamValue = "9000000000";
        private const string ValidCustomerId = "7acfc365-dfa0-6f84-46f3-eb767420aaaa";
        private const string InvalidCustomerId = "aabbcc";

        private Mock<IDocumentDBProvider> _mockDocumentDbProvider;
        private Mock<ILoggerHelper> _loggerHelper;
        private Mock<ILogger<GetDigitalIdentityByCustomerIdHttpTrigger>> _logger;

        private IGetDigitalIdentityHttpTriggerService _getDigitalIdentityByCustomerIdHttpTriggerService;
        private IHttpRequestHelper _httpRequestHelper;

        private GetDigitalIdentityByCustomerIdHttpTrigger _function;

        [SetUp]
        public void Setup()
        {
            // Mocks
            _mockDocumentDbProvider = new Mock<IDocumentDBProvider>();
            _httpRequestHelper = new HttpRequestHelper();
            _loggerHelper = new Mock<ILoggerHelper>();
            _logger = new Mock<ILogger<GetDigitalIdentityByCustomerIdHttpTrigger>>();

            _getDigitalIdentityByCustomerIdHttpTriggerService = new GetDigitalIdentityHttpTriggerService(_mockDocumentDbProvider.Object);

            _function = new GetDigitalIdentityByCustomerIdHttpTrigger(
                _getDigitalIdentityByCustomerIdHttpTriggerService,
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
            _mockDocumentDbProvider.Setup(m => m.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult(httpResponse));

            // Act
            var response = await RunFunction(ValidCustomerId, httpRequest);
            var responseResult = response as JsonResult;

            // Assert
            Assert.That(response, Is.InstanceOf<JsonResult>());
            Assert.That(responseResult.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
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
            var result = await RunFunction(ValidCustomerId, httpRequest);

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
            var result = await RunFunction(InvalidCustomerId, httpRequest);

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
            var result = await RunFunction(ValidCustomerId, httpRequest);
            var responseResult = result as ObjectResult;

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            Assert.That(responseResult.StatusCode, Is.EqualTo((int)HttpStatusCode.NoContent));
        }
        
        private async Task<IActionResult> RunFunction(string customerId, HttpRequest request)
        {
            return await _function.Run(request, customerId).ConfigureAwait(false);
        }

        private static Models.DigitalIdentity GenerateDefaultHttpResponse()
        {
            return new Models.DigitalIdentity
            {
                IdentityID = Guid.NewGuid(),
                CustomerId = Guid.Parse(ValidCustomerId),
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
