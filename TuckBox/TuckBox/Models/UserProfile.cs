using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace TuckBox.Models
{
    internal class UserProfile
    {
        [PrimaryKey]
        public string Uid { get; set; } = string.Empty; // Firebase UID
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
