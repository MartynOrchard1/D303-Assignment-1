using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;


namespace TuckBox.Models
{
    internal class DeliveryAddress
    {
        [PrimaryKey, AutoIncrement]
        public int Address_ID { get; set; }
        public string Address { get; set; } = string.Empty;

        // FK -> Users.User_ID
        [Indexed]
        public string UsersUser_ID { get; set; } = string.Empty;
    }
}
