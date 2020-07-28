using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.Interfaces
{
    public interface IValidate
    {
        Task<List<ValidationResult>> ValidateResource(IDigitalIdentity resource, bool validateModelForPost);
    }
}
