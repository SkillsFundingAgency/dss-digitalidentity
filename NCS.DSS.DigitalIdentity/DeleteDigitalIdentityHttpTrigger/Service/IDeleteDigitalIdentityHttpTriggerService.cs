using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentityHttpTrigger.Service
{
    public interface IDeleteDigitalIdentityHttpTriggerService
    {
        Task<bool> DeleteIdentityAsync(Guid identityId);
    }
}
