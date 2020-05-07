using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.GetDigitalIdentityHttpTrigger.Service
{
    public interface IGetDigitalIdentityHttpTriggerService
    {
        Task<Models.DigitalIdentity> GetIdentityAsync(Guid customerId);
    }
}
