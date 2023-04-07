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
    
    public partial class ProductRouter
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ProductRouter()
        {
            this.RouterInputs = new HashSet<RouterInput>();
        }
    
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string StationName { get; set; }
        public int StationIndex { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime CreatedAt { get; set; }
    
        public virtual Product Product { get; set; }
        public virtual User User { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RouterInput> RouterInputs { get; set; }
    }
}
