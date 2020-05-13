using System;
using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.PostDigitalIdentityHttpTrigger.Service
{
    public interface IPostDigitalIdentityHttpTriggerService
    {
        Task<Models.DigitalIdentity> CreateAsync(Models.DigitalIdentity digitalIdentity);
        Task SendToServiceBusQueueAsync(Models.DigitalIdentity digitalIdentity, string apimUrl);
    }
}