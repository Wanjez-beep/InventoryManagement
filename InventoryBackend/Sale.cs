using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    public class Sale
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = null!;

        [Required]
        public int QuantitySold { get; set; }

        [Required]
        public decimal SellingPrice { get; set; }

        [Required]
        public decimal CostPrice { get; set; }

        public decimal ProfitPerUnit => SellingPrice - CostPrice;

        public decimal TotalProfit => ProfitPerUnit * QuantitySold;

        public DateTime SaleDate { get; set; } = DateTime.Now;

        public string? Notes { get; set; }
    }
}
