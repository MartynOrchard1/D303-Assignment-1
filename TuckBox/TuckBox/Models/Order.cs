using SQLite;
using System.Collections.Generic;

namespace TuckBox.Models
{
    public class Order
    {
        [PrimaryKey]
        public string Order_ID { get; set; } = string.Empty;

        // we now store NZ formatted date, but keep DateTime? too if you want
        public string Order_Date { get; set; } = string.Empty;

        // who placed it
        [Indexed]
        public string User_ID { get; set; } = string.Empty;

        // location
        [Indexed]
        public string City_ID { get; set; } = string.Empty;
        public string? City_Name { get; set; }   // <-- NEW

        // slot
        [Indexed]
        public string Time_Slot_ID { get; set; } = string.Empty;
        public string? Time_Slot { get; set; }   // <-- NEW

        // address
        [Indexed]
        public string Address_ID { get; set; } = string.Empty;
        public string? Address { get; set; }     // <-- NEW

        // money
        public decimal Total_Price { get; set; } = 0m;

        // items
        public Dictionary<string, OrderItem>? Items { get; set; }  // <-- NEW

        // ✅ Helper for display
        [Ignore]
        public string ItemsSummary
        {
            get
            {
                if (Items == null || Items.Count == 0)
                    return "(No items)";
                return string.Join(", ",
                    Items.Values.Select(i => $"{i.Food_Name} × {i.Quantity}"));
            }
        }
    }
}
