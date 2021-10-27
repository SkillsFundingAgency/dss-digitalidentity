using AutoMapper;
using DFC.Common.Standard.Logging;
using DFC.Functions.DI.Standard;
using DFC.HTTP.Standard;
using DFC.JSON.Standard;
using DFC.Swagger.Standard;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service;
using NCS.DSS.DigitalIdentity.Interfaces;
using NCS.DSS.DigitalIdentity.Ioc;
using NCS.DSS.DigitalIdentity.Mappings;
using NCS.DSS.DigitalIdentity.Services;
using NCS.DSS.DigitalIdentity.Validation;
using System.Configuration;
using System.IO;

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
            builder.Services.AddSingleton<IDocumentDBProvider, DocumentDbProvider>();
            builder.Services.AddSingleton<IDigitalIdentityServiceBusClient, DigitalIdentitServiceBusClient>();

            builder.Services.AddScoped<ISwaggerDocumentGenerator, SwaggerDocumentGenerator>();
            builder.Services.AddScoped<IGetDigitalIdentityHttpTriggerService, GetDigitalIdentityHttpTriggerService>();
            builder.Services.AddTransient<IDigitalIdentityService, DigitalIdentityService>();
            builder.Services.AddLogging();

            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
            IConfiguration configuration = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables()
                            .Build();
            builder.Services.AddSingleton(configuration);

            //builder.Services.AddScoped<IDeleteDigitalIdentityHttpTriggerService, DeleteDigitalIdentityHttpTriggerService>();
            //builder.Services.AddScoped<IDeleteDigitalIdentityByCustomerIdHttpTriggerService, DeleteDigitalIdentityByCustomerIdHttpTriggerService>();
            //builder.Services.AddScoped<IPostDigitalIdentityHttpTriggerService, PostDigitalIdentityHttpTriggerService>();
            //builder.Services.AddScoped<IPatchDigitalIdentityHttpTriggerService, PatchDigitalIdentityHttpTriggerService>();

        }
    }
}
