using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    public class InventoryLog
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = null!;

        public int OldQuantity { get; set; }

        public int NewQuantity { get; set; }

        [Required]
        public string TransactionType { get; set; } = null!; // "Sale", "Restock", "Adjustment", "Creation"

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
