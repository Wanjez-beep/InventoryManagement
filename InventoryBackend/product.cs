using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        // Required property must be initialized or marked with the null-forgiving operator
        public string Name { get; set; } = null!;

        // Category is optional so make it nullable
        public string? Category { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}