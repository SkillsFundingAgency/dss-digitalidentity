using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.Interfaces;
using NCS.DSS.DigitalIdentity.Validation;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.UnitTests
{
    public class DeleteDigitalIdentityByCustomerIdHttpTriggerTests
    {
        private const string TouchpointIdHeaderParamKey = "touchpointId";
        private const string SubcontractorIdHeaderParamKey = "subcontractorId";
        private const string ApimUrlHeaderParameterKey = "apimurl";

        private string ApimUrlHeaderParameterValue = "http://localhost:7071/";
        private string TouchpointIdHeaderParamValue = "9000000000";
        private string SubcontractorIdHeaderParamValue = "9999999999";

        private Mock<ILogger> _mockLog;
        private Mock<IDocumentDBProvider> _mockDocumentDbProvider;
        private Mock<ILoggerHelper> _loggerHelper;

        private Mock<IDigitalIdentityService> _digitalidentityservice;
        private Mock<IDigitalIdentityServiceBusClient> _servicebus;
        private IHttpRequestHelper _httpRequestHelper;
        private IHttpResponseMessageHelper _httpResponseMessageHelper;
        private IJsonHelper _jsonHelper;
        private IValidate _validate;
        private DeleteDigitalIdentityByCustomerIdHttpTrigger.Function.DeleteDigitalIdentityByCustomerIdHttpTrigger _trigger;

        [SetUp]
        public void Setup()
        {
            // Mocks
            _mockLog = new Mock<ILogger>();
            _mockDocumentDbProvider = new Mock<IDocumentDBProvider>();
            _loggerHelper = new Mock<ILoggerHelper>();
            _digitalidentityservice = new Mock<IDigitalIdentityService>();
            _servicebus = new Mock<IDigitalIdentityServiceBusClient>();

            _validate = new Validate(_mockDocumentDbProvider.Object);
            _httpRequestHelper = new HttpRequestHelper();
            _httpResponseMessageHelper = new HttpResponseMessageHelper();
            _jsonHelper = new JsonHelper();
            _trigger = new DeleteDigitalIdentityByCustomerIdHttpTrigger.Function.DeleteDigitalIdentityByCustomerIdHttpTrigger(_digitalidentityservice.Object, _servicebus.Object, _loggerHelper.Object, _httpRequestHelper, _httpResponseMessageHelper);
        }


        private async Task<HttpResponseMessage> RunFunction(HttpRequest request, string customerId)
        {
            return await _trigger.Run(request, _mockLog.Object, customerId);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenTouchpointMissing_ThenReturnBadRequest()
        {
            // Arrange
            var customerId = Guid.NewGuid().ToString();
            var defaultRequest = new DefaultHttpRequest(new DefaultHttpContext());

            // Act
            var resp = await RunFunction(defaultRequest, customerId);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
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
            Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityDoesNotExists_ThenReturnNoContent()
        {
            // Arrange
            var customerId = Guid.NewGuid().ToString();
            var request = GenerateDefaultHttpRequest();
            _digitalidentityservice.Setup(x => x.DoesCustomerExists(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            // Act
            var resp = await RunFunction(request, customerId);

            // Assert
            Assert.AreEqual(HttpStatusCode.NoContent, resp.StatusCode);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityIsDeleted_ThenReturnOk()
        {
            // Arrange
            var customerId = Guid.NewGuid().ToString();
            var request = GenerateDefaultHttpRequest();
            _digitalidentityservice.Setup(x => x.GetIdentityForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult(new Models.DigitalIdentity() { IdentityID = new Guid() }));
            _digitalidentityservice.Setup(x => x.DeleteIdentityAsync(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _digitalidentityservice.Setup(x => x.DoesCustomerExists(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            // Act
            var resp = await RunFunction(request, customerId);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
        }


        [Test]
        public async Task GivenIdentityResourceExists_WhenIdentityIsNotDeleted_ThenReturnBadRequest()
        {
            // Arrange
            var customerId = Guid.NewGuid().ToString();
            var request = GenerateDefaultHttpRequest();
            _digitalidentityservice.Setup(x => x.GetIdentityForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult(new Models.DigitalIdentity() { IdentityID = new Guid() }));
            _digitalidentityservice.Setup(x => x.DeleteIdentityAsync(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var resp = await RunFunction(request, customerId);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Test]
        public async Task GivenIdentityResourceDoesNotExist_WhenCustomerDoesNotExist_ThenReturnBadRequest()
        {
            // Arrange
            var customerId = Guid.NewGuid().ToString();
            var request = GenerateDefaultHttpRequest();
            _digitalidentityservice.Setup(x => x.DoesCustomerExists(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var resp = await RunFunction(request, customerId);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        #region helpers
        private DefaultHttpRequest GenerateDefaultHttpRequest()
        {
            var defaultRequest = new DefaultHttpRequest(new DefaultHttpContext());

            defaultRequest.Headers.Add(TouchpointIdHeaderParamKey, TouchpointIdHeaderParamValue);
            defaultRequest.Headers.Add(SubcontractorIdHeaderParamKey, SubcontractorIdHeaderParamValue);
            defaultRequest.Headers.Add(ApimUrlHeaderParameterKey, ApimUrlHeaderParameterValue);

            return defaultRequest;
        }
        #endregion
    }
}
