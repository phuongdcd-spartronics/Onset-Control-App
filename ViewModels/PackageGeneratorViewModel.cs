using Onset_Serialization.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Onset_Serialization.ViewModels
{
    public class PackageGeneratorViewModel
    {
        public List<Product> Products { get; set; }
        public Product SelectedProduct { get; set; }
        public ObservableCollection<Package> Packages { get; private set; } = new ObservableCollection<Package>();
        public ObservableCollection<PackageData> PackageDatas { get; private set; } = new ObservableCollection<PackageData>();
        public ObservableCollection<ProductGroup> ProductGroup { get; private set; } = new ObservableCollection<ProductGroup>();
        public ProductGroup SelectedProductGroup { get; set; }
        public string PackageNumber { get; set; }
        public string PO { get; set; }
        public int SeqNo { get; set; }
        public int Quantity { get; set; }
        public int Finished { get; set; }
    }

    public class ProductGroup
    {
        public int ProductId { get; set; }
        public string PONumber { get; set; }
    }
}
