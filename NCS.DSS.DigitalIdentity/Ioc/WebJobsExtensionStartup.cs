using DFC.Common.Standard.Logging;
using DFC.Functions.DI.Standard;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using DFC.Swagger.Standard;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.DeleteDigitalIdentityByCustomerIdHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.DeleteDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityByCustomerIdHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.Ioc;
using NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.ServiceBus;
using NCS.DSS.DigitalIdentity.Validation;

[assembly: WebJobsStartup(typeof(WebJobsExtensionStartup), "Web Jobs Extension Startup")]

namespace NCS.DSS.DigitalIdentity.Ioc
{
    public class WebJobsExtensionStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddDependencyInjection();

            //builder.Services.AddSingleton<IResourceHelper, ResourceHelper>();
            builder.Services.AddSingleton<IValidate, Validate>();
            builder.Services.AddSingleton<ILoggerHelper, LoggerHelper>();
            builder.Services.AddSingleton<IHttpRequestHelper, HttpRequestHelper>();
            builder.Services.AddSingleton<IHttpResponseMessageHelper, HttpResponseMessageHelper>();
            builder.Services.AddSingleton<IJsonHelper, JsonHelper>();
            builder.Services.AddSingleton<IDocumentDBProvider, DocumentDBProvider>();
            builder.Services.AddSingleton<IDigitalIdentityServiceBusClient, DigitalIdentitServiceBusClient>();

            builder.Services.AddScoped<ISwaggerDocumentGenerator, SwaggerDocumentGenerator>();
            builder.Services.AddScoped<IGetDigitalIdentityByCustomerIdHttpTriggerService, GetDigitalIdentityByCustomerIdHttpTriggerService>();
            builder.Services.AddScoped<IGetDigitalIdentityHttpTriggerService, GetDigitalIdentityHttpTriggerService>();
            builder.Services.AddScoped<IDeleteDigitalIdentityHttpTriggerService, DeleteDigitalIdentityHttpTriggerService>();
            builder.Services.AddScoped<IDeleteDigitalIdentityByCustomerIdHttpTriggerService, DeleteDigitalIdentityByCustomerIdHttpTriggerService>();
            builder.Services.AddScoped<IPostDigitalIdentityTriggerService, PostDigitalIdentityHttpTriggerService>();
        }
    }
}
