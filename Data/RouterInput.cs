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
    
    public partial class RouterInput
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public string Pattern { get; set; }
        public System.DateTime CreatedAt { get; set; }
    
        public virtual ProductRouter ProductRouter { get; set; }
    }
}