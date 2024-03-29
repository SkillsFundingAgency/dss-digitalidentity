using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.Interfaces;
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
    public class GetDigitalIdentityHttpTriggerTests
    {
        private const string TouchpointIdHeaderParamKey = "touchpointId";
        private const string ApimUrlHeaderParameterKey = "apimurl";

        private string ApimUrlHeaderParameterValue = "http://localhost:7071/";
        private string TouchpointIdHeaderParamValue = "9000000000";
        private string validIdentityId = "fb1cd730-720a-4f3a-b160-6ff8785c37f2";
        private string invalidIdentityId = "aabbcc";

        private Mock<ILogger> _mockLog;
        private Mock<IDocumentDBProvider> _mockDocumentDbProvider;
        private Mock<ILoggerHelper> _loggerHelper;

        private IGetDigitalIdentityHttpTriggerService _getDititalIdentityHttpTriggerService;
        private IHttpRequestHelper _httpRequestHelper;
        private IHttpResponseMessageHelper _httpResponseMessageHelper;
        private IJsonHelper _jsonHelper;
        private IValidate _validate;

        [SetUp]
        public void Setup()
        {
            // Mocks
            _mockLog = new Mock<ILogger>();
            _mockDocumentDbProvider = new Mock<IDocumentDBProvider>();
            _loggerHelper = new Mock<ILoggerHelper>();

            _validate = new Validate(_mockDocumentDbProvider.Object);
            _httpRequestHelper = new HttpRequestHelper();
            _httpResponseMessageHelper = new HttpResponseMessageHelper();
            _jsonHelper = new JsonHelper();
            _getDititalIdentityHttpTriggerService = new GetDigitalIdentityHttpTriggerService(_mockDocumentDbProvider.Object);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenValidGetRequest_ThenResourceIsReturned()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest();
            var httpResponse = GenerateDefaultHttpResponse();

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult<bool>(true));
            _mockDocumentDbProvider.Setup(m => m.GetIdentityByIdentityIdAsync(It.IsAny<Guid>()))
                                                                        .Returns(Task.FromResult<Models.DigitalIdentity>(httpResponse));

            // Act
            var result = await RunFunction(validIdentityId, httpRequest);
            var actualResult = JsonConvert.DeserializeObject<Models.DigitalIdentity>(await result.Content.ReadAsStringAsync());

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual(actualResult.CustomerId, httpResponse.CustomerId);
            Assert.AreEqual(actualResult.id_token, httpResponse.id_token);
            Assert.AreEqual(actualResult.LastLoggedInDateTime, httpResponse.LastLoggedInDateTime);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenTouchPointIdMissing_ThenBadRequest()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest();
            httpRequest.Headers.Remove(TouchpointIdHeaderParamKey);

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult<bool>(true));

            // Act
            var result = await RunFunction(validIdentityId, httpRequest);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenCustomerIdIsNotGuid_ThenBadRequest()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest();

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult<bool>(true));

            // Act
            var result = await RunFunction(invalidIdentityId, httpRequest);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task GivenIdentityResourceExists_WhenCustomerDoesNotExists_ThenBadRequest()
        {
            // Arrange
            var httpRequest = GenerateDefaultHttpRequest();

            _mockDocumentDbProvider.Setup(m => m.DoesCustomerResourceExist(It.IsAny<Guid>()))
                .Returns(Task.FromResult<bool>(false));

            // Act
            var result = await RunFunction(validIdentityId, httpRequest);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        #region Helpers

        private Models.DigitalIdentity GenerateDefaultHttpResponse()
        {
            return new Models.DigitalIdentity()
            {
                IdentityID = Guid.NewGuid(),
                CustomerId = Guid.Parse(validIdentityId),
                IdentityStoreId = Guid.NewGuid(),
                LastModifiedDate = DateTime.UtcNow,
                LegacyIdentity = Guid.NewGuid().ToString(),
                id_token = "test_token",
                LastModifiedTouchpointId = TouchpointIdHeaderParamValue,
                DateOfClosure = null,
            };
        }

        private Stream GenerateStreamFromJson(string requestBody)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(requestBody);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private async Task<HttpResponseMessage> RunFunction(string customerId, HttpRequest request)
        {
            return await GetDigitalIdentityHttpTrigger.Function.GetDigitalIdentityHttpTrigger.Run(
                request,
                _mockLog.Object,
                customerId,
                _getDititalIdentityHttpTriggerService,
                _loggerHelper.Object,
                _httpRequestHelper,
                _httpResponseMessageHelper,
                _jsonHelper
            ).ConfigureAwait(false);
        }

        private DefaultHttpRequest GenerateDefaultHttpRequest()
        {
            var defaultRequest = new DefaultHttpRequest(new DefaultHttpContext());

            defaultRequest.Headers.Add(TouchpointIdHeaderParamKey, TouchpointIdHeaderParamValue);
            defaultRequest.Headers.Add(ApimUrlHeaderParameterKey, ApimUrlHeaderParameterValue);

            return defaultRequest;
        }

        #endregion
    }
}
