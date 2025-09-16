using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuckBox.Models
{
    internal class Order
    {
        [PrimaryKey]
        public string Order_ID { get; set; } = Guid.NewGuid().ToString();

        public DateTime Order_Date { get; set; } = DateTime.UtcNow;
        public int Quantity { get; set; } = 1;

        // FK's - Just id's
        [Indexed]
        public string Food_Extra_DetailsFood_Details_ID { get; set; } = string.Empty; // -> Food_Extra_Details

        [Indexed]
        public string CityCity_ID { get; set; } = string.Empty; // -> City

        [Indexed]
        public string TimeSlotsTime_Slot_ID { get; set; } = string.Empty; // -> TimeSlot

        [Indexed]
        public string UserUser_ID { get; set; } = string.Empty; // -> User

    }
}
