using System;
using System.Collections.Generic;

namespace Onset_Serialization
{
    public class UserSession
    {
        private static bool _isLogged = false;
        public static string UserID { get; set; }
        public static string Name { get; set; }
        public static List<string> Roles { get; set; }
        public static DateTime Expired { get; set; }

        public static void Create(string userID, string name, List<string> roles = null)
        {
            _isLogged = true;
            UserID = userID;
            Name = name;
            Roles = roles ?? new List<string>();
            Expired = DateTime.Now.AddDays(1);
        }

        public static bool IsLogged()
        {
            return _isLogged && Expired >= DateTime.Now;
        }

        public static void Clear()
        {
            _isLogged = false;
            UserID = null;
            Name = null;
            Roles = new List<string>();
            Expired = DateTime.Now;
        }
    }
}
