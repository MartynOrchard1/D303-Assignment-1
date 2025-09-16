using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuckBox.Models
{
    internal class Food
    {
        [PrimaryKey]
        public string Food_Id { get; set; } = Guid.NewGuid().ToString();
        [Indexed]
        public string Food_Name { get; set; } = string.Empty;

        // ERD shows a field on Foods for “Food_Extra_Choice”.
        // Keep it as optional free text; specific options live in Food_Extra_Details.
        public string? Food_Extra_Choice { get; set; }
    }

}
