using System;

namespace NCS.DSS.DigitalIdentity.Models
{
    public class Customer
    {
        public Guid id { get; set; }
        public string FamilyName { get; set; }
        public string GivenName { get; set; }
        public DateTime? DateOfTermination { get; set; }
    }
}
