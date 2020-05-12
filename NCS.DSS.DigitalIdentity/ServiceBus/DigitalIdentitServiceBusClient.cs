using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DFC.Common.Standard.ServiceBusClient.Models;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace NCS.DSS.DigitalIdentity.ServiceBus
{
    public class DigitalIdentitServiceBusClient : IDigitalIdentityServiceBusClient
    {
        public static readonly string KeyName = ConfigurationManager.AppSettings["KeyName"];
        public static readonly string AccessKey = ConfigurationManager.AppSettings["AccessKey"];
        public static readonly string BaseAddress = ConfigurationManager.AppSettings["BaseAddress"];
        public static readonly string QueueName = ConfigurationManager.AppSettings["QueueName"];

        public async Task SendPostMessageAsync(Models.DigitalIdentity digitalIdentity, string reqUrl)
        {
         string titleMessage = $"New Digital Identity record {digitalIdentity.IdentityID} added at {DateTime.UtcNow}";
         await SendMessageAsync(digitalIdentity, reqUrl, titleMessage);
        }

        public async  Task SendPatchMessageAsync(Models.DigitalIdentity updatedDigitalIdentity, string reqUrl)
        {
            string titleMessage = $"Digital Identity record modified for {updatedDigitalIdentity.IdentityID} at {DateTime.UtcNow}";
            await SendMessageAsync(updatedDigitalIdentity, reqUrl, titleMessage);
        }

        private async Task SendMessageAsync(Models.DigitalIdentity digitalIdentity, string reqUrl, string message)
        {
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(KeyName, AccessKey);
            var messagingFactory = await MessagingFactory.CreateAsync(BaseAddress, tokenProvider);
            var sender = await messagingFactory.CreateMessageSenderAsync(QueueName);

            var messageModel = new MessageModel()
            {
                TitleMessage = message,
                CustomerGuid = digitalIdentity.CustomerId,
                LastModifiedDate = digitalIdentity.LastModifiedDate,
                URL = reqUrl + "identity/" + digitalIdentity.IdentityID,
                IsNewCustomer = false,
                TouchpointId = digitalIdentity.LastModifiedTouchpointId
            };

            var msg = new BrokeredMessage(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageModel))))
            {
                ContentType = "application/json",
                MessageId = digitalIdentity.IdentityID + " " + DateTime.UtcNow //TODO : Should this be IdentityId or CustomerId ?
            };

            await sender.SendAsync(msg);
        }
    }
}
