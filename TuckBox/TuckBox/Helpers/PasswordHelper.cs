using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace TuckBox.Helpers
{
    internal class PasswordHelper
    {
        public static string HashPassword(string password)
        {
           if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or empty.");
            }
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
        
        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var hashOfEntered = HashPassword(enteredPassword);
            return hashOfEntered == storedHash;
        }
    }
}
