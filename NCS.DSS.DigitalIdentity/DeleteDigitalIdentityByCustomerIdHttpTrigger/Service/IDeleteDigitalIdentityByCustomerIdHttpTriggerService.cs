using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.DeleteDigitalIdentityByCustomerIdHttpTrigger.Service
{
    public interface IDeleteDigitalIdentityByCustomerIdHttpTriggerService
    {
        Task<bool> DeleteIdentityAsync(Guid identityId);
        Task<Models.DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId);
    }
}
