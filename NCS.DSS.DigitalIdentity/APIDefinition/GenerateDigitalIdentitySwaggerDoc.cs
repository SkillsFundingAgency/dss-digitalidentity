using DFC.Swagger.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Reflection;

namespace NCS.DSS.DigitalIdentity.APIDefinition
{
    public class GenerateOutcomeSwaggerDoc
    {
        public const string ApiTitle = "DigitalIdentities";
        public const string ApiDefinitionName = "API-Definition";
        public const string ApiDefRoute = ApiTitle + "/" + ApiDefinitionName;
        public const string ApiDescription = "Internal API that connects Accounts, DSS and DfE signin";

        private readonly ISwaggerDocumentGenerator _swaggerDocumentGenerator;
        public const string ApiVersion = "3.0.0";

        public GenerateOutcomeSwaggerDoc(ISwaggerDocumentGenerator swaggerDocumentGenerator)
        {
            _swaggerDocumentGenerator = swaggerDocumentGenerator;
        }

        [Function(ApiDefinitionName)]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiDefRoute)]HttpRequest req)
        {
            var swaggerDoc = _swaggerDocumentGenerator.GenerateSwaggerDocument(req, ApiTitle, ApiDescription,
                ApiDefinitionName, ApiVersion, Assembly.GetExecutingAssembly());

            if (string.IsNullOrEmpty(swaggerDoc))
                return new NoContentResult();

            return new OkObjectResult(swaggerDoc);
        }
    }
}
