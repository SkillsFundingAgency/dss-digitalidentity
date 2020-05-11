using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.ServiceBus
{
    public interface IDigitalIdentityServiceBusClient
    {
        Task SendPostMessageAsync(Models.DigitalIdentity digitalIdentity, string reqUrl);
    }
}