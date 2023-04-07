using Onset_Serialization.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Onset_Serialization.ViewModels
{
    public class ScanViewModel
    {
        public class ScanData : INotifyPropertyChanged
        {
            public int No { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public RouterInput Input { get; set; }

#pragma warning disable 67
            public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67
        }

        public List<ScanData> ScanList { get; private set; } = new List<ScanData>();
        public bool IsEmpty { get { return ScanList.Count == 0; } }

        public ScanViewModel() 
        {
            ScanList.Add(new ScanData() { Name = "*" });
        }

        public ScanViewModel(List<RouterInput> router)
        {
            foreach (var route in router.OrderBy(x => x.Index))
            {
                ScanList.Add(new ScanData()
                {
                    No = route.Index,
                    Name = $"{route.Index}. {route.Name}",
                    Input = route
                });
            }
        }

        public bool Append(string value)
        {
            ScanData data = ScanList.FirstOrDefault(x => x.Value == null);
            if (data == null)
                return false;
            if (!Regex.IsMatch(value, data.Input.Pattern))
            {
                throw new FormatException($"`{value}` không đúng định dạng `{data.Input.Name}`");
            }

            data.Value = value;
            return true;
        }

        public bool IsFull()
        {
            return !ScanList.Any(x => x.Value == null);
        }

        public void Clear()
        {
            for (int i = 0; i < ScanList.Count; i++)
            {
                ScanList[i].Value = null;
            }
        }
    }
}
