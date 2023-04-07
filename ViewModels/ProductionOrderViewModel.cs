using Onset_Serialization.Data;
using Onset_Serialization.Enums;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Onset_Serialization.ViewModels
{
    public class ProductionOrderViewModel : INotifyPropertyChanged
    {
        public ProductionOrder Order { get; set; }
        public List<Product> ProductList { get; set; }
        public ObservableCollection<ProductionOrder> OrderList { get; set; }
        public bool IsNew { get; set; }
        public bool AllowEdit { get { return IsNew || Order?.Status == Status.CREATED; } }

#pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67
    }
}
