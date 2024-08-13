using System.ComponentModel.DataAnnotations;

namespace NCS.DSS.DigitalIdentity.Interfaces
{
    public interface IValidate
    {
        Task<List<ValidationResult>> ValidateResource(IDigitalIdentity resource, bool validateModelForPost);
    }
}
