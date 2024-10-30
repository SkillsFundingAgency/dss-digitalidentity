using Microsoft.Azure.ServiceBus;
using NCS.DSS.DigitalIdentity.Interfaces;
using Newtonsoft.Json;
using System.Text;

namespace NCS.DSS.DigitalIdentity.Services
{
    public class DigitalIdentityServiceBusClient : IDigitalIdentityServiceBusClient
    {
        public static readonly string KeyName = Environment.GetEnvironmentVariable("KeyName");
        public static readonly string AccessKey = Environment.GetEnvironmentVariable("AccessKey");
        public static readonly string BaseAddress = Environment.GetEnvironmentVariable("BaseAddress");
        public static readonly string QueueName = Environment.GetEnvironmentVariable("QueueName");
        public static readonly string ServiceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");

        public async Task SendPostMessageAsync(Models.DigitalIdentity digitalIdentity, string reqUrl)
        {
            string titleMessage = $"New Digital Identity record {digitalIdentity.IdentityID} added at {DateTime.UtcNow}";
            await SendMessageAsync(digitalIdentity, reqUrl, titleMessage);
        }

        public async Task SendDeleteMessageAsync(Models.DigitalIdentity updatedDigitalIdentity, string reqUrl)
        {
            string titleMessage = $"Digital Identity deleted for {updatedDigitalIdentity.IdentityID} at {DateTime.UtcNow}";
            await SendMessageAsync(updatedDigitalIdentity, reqUrl, titleMessage);
        }

        private async Task SendMessageAsync(Models.DigitalIdentity digitalIdentity, string reqUrl, string message)
        {
            var queueClient = new QueueClient(ServiceBusConnectionString, QueueName);
            var sbMessage = new
            {
                TitleMessage = message,
                CustomerGuid = digitalIdentity.CustomerId,
                digitalIdentity.LastModifiedDate,
                URL = reqUrl + "identity/" + digitalIdentity.IdentityID,
                IsNewCustomer = false,
                TouchpointId = digitalIdentity.LastModifiedTouchpointId,
                FirstName = digitalIdentity.FirstName,
                digitalIdentity.LastName,
                digitalIdentity.EmailAddress,
                digitalIdentity.CustomerId,
                CreateDigitalIdentity = digitalIdentity.CreateDigitalIdentity ?? false,
                IsDigitalAccount = digitalIdentity.IsDigitalAccount ?? false,
                DeleteDigitalIdentity = digitalIdentity.DeleteDigitalIdentity ?? false,
                IdentityStoreId = digitalIdentity.IdentityStoreId ?? null,
                DoB = digitalIdentity.DoB ?? null
            };
            var msg = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sbMessage)));
            await queueClient.SendAsync(msg);
        }
    }
}
