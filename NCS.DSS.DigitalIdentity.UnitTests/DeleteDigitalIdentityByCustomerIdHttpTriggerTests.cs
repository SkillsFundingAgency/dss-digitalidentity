using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.DigitalIdentity.Interfaces;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.UnitTests
{
    public class DeleteDigitalIdentityByCustomerIdHttpTriggerTests
    {
        private const string TouchpointIdHeaderParamKey = "touchpointId";
        private const string ApimUrlHeaderParameterKey = "apimurl";
        private const string ApimUrlHeaderParameterValue = "http://localhost:7071/";
        private const string TouchpointIdHeaderParamValue = "9000000000";

        private Mock<IDigitalIdentityService> _digitalIdentityService;
        private Mock<IDigitalIdentityServiceBusClient> _serviceBus;
        private Mock<ILoggerHelper> _loggerHelper;
        private Mock<ILogger<DeleteDigitalIdentityByCustomerIdHttpTrigger.Function.DeleteDigitalIdentityByCustomerIdHttpTrigger>> _logger;

        private IHttpRequestHelper _httpRequestHelper;

        private DeleteDigitalIdentityByCustomerIdHttpTrigger.Function.DeleteDigitalIdentityByCustomerIdHttpTrigger _trigger;

        [SetUp]
        public void Setup()
        {
            // Mocks
            _logger = new Mock<ILogger<DeleteDigitalIdentityByCustomerIdHttpTrigger.Function.DeleteDigitalIdentityByCustomerIdHttpTrigger>>();
            _loggerHelper = new Mock<ILoggerHelper>();
            _digitalIdentityService = new Mock<IDigitalIdentityService>();
            _serviceBus = new Mock<IDigitalIdentityServiceBusClient>();

            _httpRequestHelper = new HttpRequestHelper();
            _trigger = new DeleteDigitalIdentityByCustomerIdHttpTrigger.Function.DeleteDigitalIdentityByCustomerIdHttpTrigger(
                _digitalIdentityService.Object,
                _serviceBus.Object,
                _httpRequestHelper,
                _loggerHelper.Object,
                _logger.Object);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenTouchpointMissing_ThenReturnBadRequest()
        {
            // Arrange
            var customerId = Guid.NewGuid().ToString();
            var defaultRequest = new DefaultHttpContext().Request;

            // Act
            var resp = await RunFunction(defaultRequest, customerId);

            // Assert
            Assert.That(resp, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public async Task GivenInvalidRequest_WhenCustomerIdMissing_ThenReturnBadRequest()
        {
            // Arrange
            var customerId = "";
            var request = GenerateDefaultHttpRequest();

            // Act
            var resp = await RunFunction(request, customerId);

            // Assert
            Assert.That(resp, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityDoesNotExists_ThenReturnNoContent()
        {
            // Arrange
            var customerId = Guid.NewGuid().ToString();
            var request = GenerateDefaultHttpRequest();
            _digitalIdentityService.Setup(x => x.DoesCustomerExists(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            // Act
            var resp = await RunFunction(request, customerId);

            // Assert
            Assert.That(resp, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityIsDeleted_ThenReturnOk()
        {
            // Arrange
            var customerId = Guid.NewGuid().ToString();
            var request = GenerateDefaultHttpRequest();
            _digitalIdentityService.Setup(x => x.GetIdentityForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult(new Models.DigitalIdentity { IdentityID = new Guid() }));
            _digitalIdentityService.Setup(x => x.DeleteIdentityAsync(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _digitalIdentityService.Setup(x => x.DoesCustomerExists(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            // Act
            var resp = await RunFunction(request, customerId);

            // Assert
            Assert.That(resp, Is.InstanceOf<OkResult>());
        }


        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityIsNotDeleted_ThenReturnBadRequest()
        {
            // Arrange
            var customerId = Guid.NewGuid().ToString();
            var request = GenerateDefaultHttpRequest();
            _digitalIdentityService.Setup(x => x.GetIdentityForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult(new Models.DigitalIdentity { IdentityID = new Guid() }));
            _digitalIdentityService.Setup(x => x.DeleteIdentityAsync(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var resp = await RunFunction(request, customerId);

            // Assert
            Assert.That(resp, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public async Task GivenIdentityResourceDoesNotExist_WhenCustomerDoesNotExist_ThenReturnBadRequest()
        {
            // Arrange
            var customerId = Guid.NewGuid().ToString();
            var request = GenerateDefaultHttpRequest();
            _digitalIdentityService.Setup(x => x.DoesCustomerExists(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var resp = await RunFunction(request, customerId);

            // Assert
            Assert.That(resp, Is.InstanceOf<BadRequestResult>());
        }

        private async Task<IActionResult> RunFunction(HttpRequest request, string customerId)
        {
            return await _trigger.Run(request, customerId);
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
