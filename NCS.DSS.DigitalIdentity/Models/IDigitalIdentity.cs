using System;

namespace NCS.DSS.DigitalIdentity.Models
{
    public interface IDigitalIdentity
    {
        string LegacyIdentity { get; set; }
        string id_token { get; set; }
        DateTime? LastLoggedInDateTime { get; set; }
        DateTime? LastModifiedDate { get; set; }
        string LastModifiedTouchpointId { get; set; }
        Guid? CustomerId { get; set; }
        string EmailAddress { get; }
        string FirstName { get; }
        string LastName { get;  }
        bool? CreateDigitalIdentity { get; }

        void SetDefaultValues();
        void SetDigitalIdentity(string emailAddress, string firstName, string lastName);
    }
}