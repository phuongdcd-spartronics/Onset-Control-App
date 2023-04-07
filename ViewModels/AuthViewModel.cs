using System.Collections.Generic;
using System.ComponentModel;

namespace Onset_Serialization.ViewModels
{
    public class AuthViewModel : INotifyPropertyChanged
    {
        public string UserName { get; set; }
        public string Name { get; set; }
        public List<string> Roles { get; set; }
        public string Identifier { get; set; }
        public bool IsLogged { get; set; }

#pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67
    }
}
