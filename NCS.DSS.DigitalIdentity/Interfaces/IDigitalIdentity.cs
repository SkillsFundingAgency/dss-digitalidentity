﻿using System;
using System.Runtime.Serialization;

namespace NCS.DSS.DigitalIdentity.Interfaces
{
    public interface IDigitalIdentity
    {
        string LegacyIdentity { get; set; }
        string id_token { get; set; }
        [IgnoreDataMember]
        DateTime? LastLoggedInDateTime { get; set; }
        DateTime? LastModifiedDate { get; set; }
        string LastModifiedTouchpointId { get; set; }
        Guid CustomerId { get; set; }
    }
}