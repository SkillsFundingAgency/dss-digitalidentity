using System;

namespace NCS.DSS.DigitalIdentity.Models
{
    public interface IDigitalIdentity
    {
        public string LegacyIdentity { get; set; }
        string id_token { get; set; }
        DateTime? LastLoggedInDateTime { get; set; }
        DateTime? LastModifiedDate { get; set; }
        string LastModifiedTouchpointId { get; set; }

        void SetDefaultValues();
    }
}