using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace copilot_deneme
{
    public static class AuthService
    {
        public static bool IsSignedIn { get; private set; } = false;

        public static bool SignIn(string username, string password)
        {
            // Simple check: username = "user", password = "pass"
            if (username == "Taha" && password == "deneme")
            {
                
                IsSignedIn = true;
                return true;
            }
            return false;
        }

        public static void SignOut()
        {
            IsSignedIn = false;
        }
    }
}
