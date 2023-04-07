using System;
using System.ComponentModel;

namespace Onset_Serialization.Models
{
    public class PrintLogInfo : INotifyPropertyChanged
    {
        public int? PrintId { get; set; }
        public Guid RefId { get; set; }
        public string SerialNumber { get; set; }
        public int StationIndex { get; set; }
        public string StationName { get; set; }
        public string Status { get; set; }
        public string PrintedLabel { get; set; }
        public string PrintedData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModified { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
