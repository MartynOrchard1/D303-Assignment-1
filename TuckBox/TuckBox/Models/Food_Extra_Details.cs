using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace TuckBox.Models
{
    internal class Food_Extra_Details
    {
        [PrimaryKey]
        public string Food_Details_ID { get; set; } = Guid.NewGuid().ToString();

        [Indexed]
        public string Details_Name { get; set; } = string.Empty;

        // FK -> food.food_id
        [Indexed]
        public string FoodFood_ID { get; set; } = string.Empty;
    }
}
