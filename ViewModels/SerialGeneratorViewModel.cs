using Onset_Serialization.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Onset_Serialization.ViewModels
{
    public class SerialGeneratorViewModel
    {
        public List<Product> Products { get; set; }
        public ObservableCollection<SerialData> SerialData { get; private set; } = new ObservableCollection<SerialData>();

        public Product SelectedProduct { get; set; }
        public string PO { get; set; }
        public int From { get; set; }
        public int To { get; set; }
    }
}
