using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// Models/Food.cs
namespace TuckBox.Models
{
    public class Food
    {
        public string Food_ID { get; set; } = string.Empty;
        public string Food_Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; } = 0.0;

        // Customisation
        public string Option_Key { get; set; } = string.Empty;     // e.g. "dressing", "spice"
        public List<string> Option_Values { get; set; } = new();   // e.g. ["mild", "med", "hot"]
    }
}

