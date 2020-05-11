using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using NCS.DSS.DigitalIdentity.Models;

namespace NCS.DSS.DigitalIdentity.Validation
{
    public interface IValidate
    {
        Task<List<ValidationResult>> ValidateResource(IDigitalIdentity resource, bool validateModelForPost);
    }
}
