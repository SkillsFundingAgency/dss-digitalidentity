using DFC.Swagger.Standard.Annotations;
using NCS.DSS.DigitalIdentity.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace NCS.DSS.DigitalIdentity.Models
{
    public class DigitalIdentityPatch : IDigitalIdentity
    {
        [Display(Description = "Unique identifier of a identity store.")]
        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        public Guid? IdentityStoreID { get; set; }

        [Display(Description = "Unique identifier of a customer.")]
        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        public Guid? CustomerId { get; set; }

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

        public string EmailAddress { get; private set; }

        public string FirstName { get; private set; }

        public string LastName { get; private set; }
        public bool? CreateDigitalIdentity { get; private set; }

        public bool? IsDigitalAccount =>  null;

        public bool? DeleteDigitalIdentity => null;

        public void SetCreateDigitalIdentity(string emailAddress, string firstName, string lastName)
        {
            throw new NotImplementedException();
        }

        public void SetDefaultValues()
        {
            if (!LastModifiedDate.HasValue)
                LastModifiedDate = DateTime.UtcNow;

        }

        public void SetDeleted()
        {
            throw new NotImplementedException();
        }

        public void SetDigitalIdentity(string emailAddress, string firstName, string lastName)
        {
            throw new NotImplementedException();
        }
    }
}
