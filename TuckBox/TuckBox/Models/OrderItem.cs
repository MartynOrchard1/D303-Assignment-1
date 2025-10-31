namespace TuckBox.Models
{
    public class OrderItem
    {
        public string Food_ID { get; set; } = string.Empty;
        public string Food_Name { get; set; } = string.Empty;
        public int Quantity { get; set; } = 0;
        public string? Option_Key { get; set; }
        public string? Option_Value { get; set; }
        public decimal Unit_Price { get; set; } = 0m;
        public decimal Line_Total { get; set; } = 0m;
    }
}
