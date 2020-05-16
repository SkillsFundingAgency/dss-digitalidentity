using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using NCS.DSS.DigitalIdentity.Models;

namespace NCS.DSS.DigitalIdentity.Validation
{
    public class Validate : IValidate
    {
        private readonly IDocumentDBProvider _documentDbProvider;

        public Validate(IDocumentDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public async Task<List<ValidationResult>> ValidateResource(Models.IDigitalIdentity resource, bool validateModelForPost)
        {
            var context = new ValidationContext(resource, null, null);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(resource, context, results, true);

            await ValidateCustomerRules(resource, results, validateModelForPost);

            return results;
        }

        private async Task ValidateCustomerRules(Models.IDigitalIdentity resource, List<ValidationResult> results, bool validateModelForPost)
        {
            
        }
    }
}
