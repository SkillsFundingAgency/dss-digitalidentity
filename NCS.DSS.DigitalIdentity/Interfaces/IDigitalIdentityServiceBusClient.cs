namespace NCS.DSS.DigitalIdentity.Interfaces
{
    public interface IDigitalIdentityServiceBusClient
    {
        Task SendPostMessageAsync(Models.DigitalIdentity digitalIdentity, string reqUrl);
        Task SendDeleteMessageAsync(Models.DigitalIdentity updatedDigitalIdentity, string reqUrl);
    }
}