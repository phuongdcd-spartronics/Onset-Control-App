//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Onset_Serialization.Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class SerialRouting
    {
        public System.Guid Id { get; set; }
        public string SerialNumber { get; set; }
        public int OrderId { get; set; }
        public int StationIndex { get; set; }
        public string StationName { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedAt { get; set; }
    
        public virtual SerialData SerialData { get; set; }
        public virtual ProductionOrder ProductionOrder { get; set; }
    }
}
