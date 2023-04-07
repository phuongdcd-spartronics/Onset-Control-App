using System;
using System.ComponentModel;
using System.Windows.Media;

namespace Onset_Serialization.Models
{
    public class ScanDataInfo : INotifyPropertyChanged
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public bool Status { get; set; }
        public DateTime Time { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
