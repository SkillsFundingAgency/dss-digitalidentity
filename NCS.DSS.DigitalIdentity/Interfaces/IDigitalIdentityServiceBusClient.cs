using System.Threading.Tasks;

namespace NCS.DSS.DigitalIdentity.Interfaces
{
    public interface IDigitalIdentityServiceBusClient
    {
        Task SendPostMessageAsync(Models.DigitalIdentity digitalIdentity, string reqUrl);

        Task SendPatchMessageAsync(Models.DigitalIdentity updatedDigitalIdentity, string reqUrl);
    }
}