using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuckBox.Models
{
    public class City
    {
        [PrimaryKey]
        public string City_ID { get; set; } = Guid.NewGuid().ToString();

        [Indexed]
        public string City_Name { get; set; } = string.Empty;
    }
}
