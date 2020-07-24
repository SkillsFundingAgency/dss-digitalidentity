using NCS.DSS.DigitalIdentity.Cosmos.Provider;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

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
            //customer
            var existingCustomer = await _documentDbProvider.GetCustomer(resource.CustomerId.GetValueOrDefault());
            if (existingCustomer == null)
                results.Add(new ValidationResult($"Customer with CustomerId  {resource.CustomerId.GetValueOrDefault()} does not exists."));
            else
            {
                if (existingCustomer.DateOfTermination.HasValue)
                    results.Add(new ValidationResult($"Unable to create DigitalIdentity for CustomerId  {resource.CustomerId.GetValueOrDefault()}, customer is readonly."));
            }

            //only validate through posting a new digital identity
            var digitalIdentity = await _documentDbProvider.GetIdentityForCustomerAsync(resource.CustomerId.GetValueOrDefault());
            if (digitalIdentity != null)
                results.Add(new ValidationResult($"Digital Identity for CustomerId {resource.CustomerId.GetValueOrDefault()} already exists."));

            //email address check, will need to revisit for patches
            if (!string.IsNullOrEmpty(resource.EmailAddress))
            {
                var doesContactWithEmailExists = await _documentDbProvider.DoesContactDetailsWithEmailExists(resource.EmailAddress);
                if (doesContactWithEmailExists)
                    results.Add(new ValidationResult($"Contact with Email Address {resource.EmailAddress} already exists.", new[] { "EmailAddress" }));
            }
        }
    }
}
