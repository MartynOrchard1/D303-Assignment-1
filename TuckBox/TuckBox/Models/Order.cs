using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuckBox.Models
{
    public class Order
    {
        [PrimaryKey]
        public string Order_ID { get; set; } = Guid.NewGuid().ToString();

        // Core order info
        public DateTime Order_Date { get; set; } = DateTime.UtcNow;
        public int Quantity { get; set; } = 1;

        // Foreign keys
        [Indexed]
        public string Food_ID { get; set; } = string.Empty;  // -> Foods.Food_ID

        [Indexed]
        public string City_ID { get; set; } = string.Empty;  // -> Cities.City_ID

        [Indexed]
        public string TimeSlot_ID { get; set; } = string.Empty;  // -> TimeSlots.TimeSlot_ID

        [Indexed]
        public string User_ID { get; set; } = string.Empty;  // -> Users.User_ID (Firebase UID)

        // Extra details or customisation (like spice level, dressing, etc.)
        public string Option_Key { get; set; } = string.Empty;
        public string Option_Value { get; set; } = string.Empty;

        // Price snapshot at order time
        public double Total_Price { get; set; } = 0.0;
    }
}

