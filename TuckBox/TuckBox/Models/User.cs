using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuckBox.Models
{
    public class User
    {
        [PrimaryKey]
        public string User_ID { get; set; } = Guid.NewGuid().ToString();

        [Indexed(Unique = true)]
        public string User_Email { get; set; } = string.Empty;

        // If you later using Firebase Auth, keep this empty or store a hash/token reference.
        public string Password { get; set; } = string.Empty;
        public string First_Name { get; set; } = string.Empty;
        public string Last_Name { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
    }
}
