using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using NCS.DSS.DigitalIdentity;

namespace NCS.DSS.DigitalIdentity.Validation
{
    public interface IValidate
    {
        Task<List<ValidationResult>> ValidateResource(Models.IDigitalIdentity resource, bool validateModelForPost);
    }
}
