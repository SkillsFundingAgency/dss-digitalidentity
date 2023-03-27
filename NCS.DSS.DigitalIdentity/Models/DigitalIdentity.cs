using DFC.JSON.Standard.Attributes;
using DFC.Swagger.Standard.Annotations;
using NCS.DSS.DigitalIdentity.DTO;
using NCS.DSS.DigitalIdentity.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace NCS.DSS.DigitalIdentity.Models
{
    public class DigitalIdentity 
    {
        [Display(Description = "Unique identifier of a identity store.")]
        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public Guid? IdentityID { get; set; }

        [Required]
        [Display(Description = "Unique identifier of a customer.")]
        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        public Guid CustomerId { get; set; }

        public Guid? IdentityStoreId { get; set; }


        [Display(Description = "Unique identifier as used by legacy live services.")]
        public string LegacyIdentity { get; set; }

        public string id_token { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Description = "Date and time the customer last signed in through the digital service.")]
        [Example(Description = "2018-06-20T13:45:00")]
        [IgnoreDataMember]
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
        public DateTime? DateOfClosure { get; set; }

        public string CreatedBy { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "ttl", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int? ttl { get; set; }

        [IgnoreDataMember]
        [JsonIgnoreOnSerialize]
        public string EmailAddress { get; private set; }
        [IgnoreDataMember]
        [JsonIgnoreOnSerialize]
        public string FirstName { get; private set; }
        [IgnoreDataMember]
        [JsonIgnoreOnSerialize]
        public string LastName { get; private set; }
        [IgnoreDataMember]
        [JsonIgnoreOnSerialize]
        public bool? CreateDigitalIdentity { get; private set; }
        [IgnoreDataMember]
        [JsonIgnoreOnSerialize]
        public bool? IsDigitalAccount { get; private set; }
        [IgnoreDataMember]
        [JsonIgnoreOnSerialize]
        public bool? DeleteDigitalIdentity { get; private set; }
        [IgnoreDataMember]
        [JsonIgnoreOnSerialize]
        public DateTime? DoB { get; private set; }

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
            CreateDigitalIdentity = false;
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
                LastLoggedInDateTime = DateTime.UtcNow;
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
