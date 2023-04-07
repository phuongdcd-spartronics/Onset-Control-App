using Onset_Serialization.Data;
using System.Collections.Generic;
using System.Linq;

namespace Onset_Serialization.Utilities
{
    public class AuthUtils
    {
        public const string ACTION_DELETE = "deletion";
        public const string ACTION_REPRINT = "reprint";
        public const string ACTION_RESET_ROUTE = "reset_route";
        public const string ACTION_SETUP = "setup";

        private IDictionary<string, string> _authDict = new Dictionary<string, string>();

        public AuthUtils(List<Authorization> authList)
        {
            UpdateAuth(authList);
        }

        public void UpdateAuth(List<Authorization> authList)
        {
            _authDict.Clear();
            foreach (Authorization auth in authList)
            {
                _authDict.Add(auth.ActionName, auth.Roles);
            }
        }

        public bool Authenticate(string action, out string authID)
        {
            if (UserSession.IsLogged() && IsGranted(action, UserSession.Roles))
            {
                authID = UserSession.UserID;
                return true;
            }
            else
            {
                AuthWindow auth = new AuthWindow();
                auth.ShowDialog();
                if (auth.vm.IsLogged)
                {
                    List<string> roles = auth.vm.Roles;
                    authID = auth.vm.UserName;
                    if (IsGranted(action, roles))
                    {
                        return true;
                    }
                    else
                    {
                        CstMessageBox.Show("Access denied", "You don't have permission to execute this action", CstMessageBoxIcon.Error);
                    }
                }
            }
            authID = null;
            return false;
        }

        public bool IsGranted(string action, List<string> roles)
        {
            if (!_authDict.ContainsKey(action))
            {
                return false;
            }

            string[] authRoles = _authDict[action].Split(';');
            return authRoles.Intersect(roles).Count() > 0;
        }
    }
}
