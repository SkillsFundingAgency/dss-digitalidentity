using DFC.JSON.Standard.Attributes;
using DFC.Swagger.Standard.Annotations;
using NCS.DSS.DigitalIdentity.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace NCS.DSS.DigitalIdentity.Models
{
    public class DigitalIdentity : IDigitalIdentity
    {
        public DigitalIdentity()
        {
        }

        [Display(Description = "Unique identifier of a digital identity")]
        [Example(Description = "b8592ff8-af97-49ad-9fb2-e5c3c717fd85")]
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public Guid? IdentityID { get; set; }

        [Display(Description = "Unique identifier of a customer.")]
        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        public Guid? CustomerId { get; set; }

        [Display(Description = "Unique identifier of a identity store.")]
        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        public Guid? IdentityStoreId { get; set; }

        [Display(Description = "Unique identifier as used by legacy live services.")]
        public string LegacyIdentity { get; set; }

        [Display(Description = "The JWT token returned by the identity provider following a successful authentication. ")]
        public string id_token { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Description = "Date and time the customer last signed in through the digital service.")]
        [Example(Description = "2018-06-20T13:45:00")]
        public DateTime? LastLoggedInDateTime { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Description = "Date and time of the last modification to the record.")]
        [Example(Description = "2018-06-20T13:45:00")]
        public DateTime? LastModifiedDate { get; set; }

        [StringLength(10, MinimumLength = 10)]
        [Display(Description = "Identifier of the touchpoint who made the last change to the record")]
        [Example(Description = "9000000000")]
        public string LastModifiedTouchpointId { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Description = "Date and time the resource was terminated.")]
        [Example(Description = "2018-06-20T13:45:00")]
        public DateTime? DateOfTermination { get; set; }

        [JsonIgnoreOnSerialize]
        public string CreatedBy { get; set; }

        [JsonIgnoreOnSerialize]
        public string EmailAddress { get; private set; }
        [JsonIgnoreOnSerialize]
        public string FirstName { get; private set; }
        [JsonIgnoreOnSerialize]
        public string LastName { get; private set; }
        [JsonIgnoreOnSerialize]
        public bool? CreateDigitalIdentity { get; private set; }
        [JsonIgnoreOnSerialize]
        public bool? IsDigitalAccount { get; private set; }
        [JsonIgnoreOnSerialize]
        public bool? DeleteDigitalIdentity { get; private set; }

        public void SetDefaultValues()
        {
            if (!LastModifiedDate.HasValue)
                LastModifiedDate = DateTime.UtcNow;
        }

        public void SetCreateDigitalIdentity(string emailAddress, string firstName, string lastName)
        {
            EmailAddress = emailAddress;
            FirstName = firstName;
            LastName = lastName;
            if (!string.IsNullOrEmpty(emailAddress) && !string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
            {
                IsDigitalAccount = true;
                CreateDigitalIdentity = true;
            }
        }

        public void SetDeleted()
        {
            DeleteDigitalIdentity = true;
            IsDigitalAccount = true;
        }

        public void Patch(DigitalIdentityPatch identityRequestPatch)
        {
            if (identityRequestPatch == null)
                return;

            if (identityRequestPatch.IdentityStoreID.HasValue)
            {
                IdentityStoreId = identityRequestPatch.IdentityStoreID;
            }

            if (identityRequestPatch.LastLoggedInDateTime.HasValue)
            {
                LastLoggedInDateTime = identityRequestPatch.LastLoggedInDateTime;
            }

            if (!string.IsNullOrEmpty(identityRequestPatch.LegacyIdentity))
            {
                LegacyIdentity = identityRequestPatch.LegacyIdentity;
            }

            if (!string.IsNullOrEmpty(identityRequestPatch.id_token))
            {
                id_token = identityRequestPatch.id_token;
            }

            LastModifiedTouchpointId = identityRequestPatch.LastModifiedTouchpointId;
            LastModifiedDate = DateTime.UtcNow;

        }
    }
}
