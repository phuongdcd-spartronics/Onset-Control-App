using System;

namespace Onset_Serialization.Api.Models
{
    public class UserData
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Gender { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public DateTime JoinedDate { get; set; }
        public string Role { get; set; }
    }
}
