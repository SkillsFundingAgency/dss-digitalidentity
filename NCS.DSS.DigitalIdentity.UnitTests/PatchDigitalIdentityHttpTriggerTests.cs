using System;
using System.Net.Http;
using System.Threading.Tasks;
using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using NCS.DSS.DigitalIdentity.Cosmos.Helper;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityByCustomerIdHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.PatchDigitalIdentityHttpTrigger.Function;
using NCS.DSS.DigitalIdentity.PatchDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.Validation;
using NSubstitute;
using NUnit.Framework;
using Xunit;

namespace NCS.DSS.DigitalIdentity.UnitTests
{
    [TestFixture]
    public class PatchDigitalIdentityByIdentityIdHttpTriggerTests
    {
        private const string valid = "7E467BDB-213F-407A-B86A-1954053D3C24";
        private const string IdentityId = "1111111-2222-3333-4444-555555555555";
        private IResourceHelper _resourceHelper;
        private IPatchDigitalIdentityHttpTriggerService _patchDigitalIdentityHttpTriggerService;
        private IGetDigitalIdentityByCustomerIdHttpTriggerService _getDigitalIdentityByCustomerIdHttpTriggerService;
        private ILoggerHelper _loggerHelper;
        private IHttpRequestHelper _httpRequestHelper;
        private IHttpResponseMessageHelper _httpResponseMessageHelper;
        private IJsonHelper _jsonHelper;
        private IValidate _validate;

        private ILogger _log;
        private HttpRequest _request;

        [SetUp]
        public void Setup()
        {
            _request = new DefaultHttpRequest(new DefaultHttpContext());

            _log = Substitute.For<ILogger>();
            _resourceHelper = Substitute.For<IResourceHelper>();
            _validate = Substitute.For<IValidate>();
            _loggerHelper = Substitute.For<ILoggerHelper>();
            _httpRequestHelper = Substitute.For<IHttpRequestHelper>();
            _httpResponseMessageHelper = Substitute.For<IHttpResponseMessageHelper>();
            _jsonHelper = Substitute.For<IJsonHelper>();
            _log = Substitute.For<ILogger>();
            _resourceHelper = Substitute.For<IResourceHelper>();
            _patchDigitalIdentityHttpTriggerService = Substitute.For<IPatchDigitalIdentityHttpTriggerService>();
            _getDigitalIdentityByCustomerIdHttpTriggerService = Substitute.For<IGetDigitalIdentityByCustomerIdHttpTriggerService>();

            _httpRequestHelper.GetDssTouchpointId(_request).Returns("0000000001");
            _httpRequestHelper.GetDssApimUrl(_request).Returns("http://localhost:7071/");
            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);
        }

        [Fact]
        public void GivenIdentityResourceExists_WhenValidPatchRequest_ThenResourceIsUpdated()
        {
            // 
        }

        private async Task<HttpResponseMessage> RunFunction(string identityId)
        {
            return await PatchDigitalIdentityHttpTrigger.Function.PatchDigitalIdentityByIdentityIdHttpTrigger.RunAsync(
                _request,
                _log,
                identityId,
                _resourceHelper,
                _patchDigitalIdentityHttpTriggerService,
                _getDigitalIdentityByCustomerIdHttpTriggerService,
                _loggerHelper,
                _httpRequestHelper,
                _httpResponseMessageHelper,
                _jsonHelper,
                _validate
            ).ConfigureAwait(false);
        }
    }
}
