using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuckBox.Models
{
    internal class TimeSlot
    {
        [PrimaryKey]
        public string TimeSlot_ID { get; set; } = Guid.NewGuid().ToString();

        [Indexed]
        public string Time_Slot { get; set; } = string.Empty;
    }
}
