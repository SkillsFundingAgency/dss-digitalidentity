﻿using DFC.Swagger.Standard.Annotations;
using NCS.DSS.DigitalIdentity.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace NCS.DSS.DigitalIdentity.DTO
{
    public class DigitalIdentityPatch : IDigitalIdentity
    {
        [Display(Description = "Unique identifier of a identity store.")]
        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        public Guid? IdentityStoreID { get; set; }

        [Required]
        [Display(Description = "Unique identifier of a customer.")]
        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        public Guid CustomerId { get; set; }

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
        public DateTime? DateOfClosure { get; set; }
    }
}
