using Microsoft.AspNetCore.Mvc;
using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            return Ok(await _context.Products.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Log the creation
            var log = new InventoryLog
            {
                ProductId = product.Id,
                ProductName = product.Name,
                OldQuantity = 0,
                NewQuantity = product.Quantity,
                TransactionType = "Creation",
                Notes = "Product created"
            };
            _context.InventoryLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing == null) return NotFound();

            int oldQuantity = existing.Quantity;

            existing.Name = product.Name;
            existing.Category = product.Category;
            existing.Quantity = product.Quantity;
            existing.Price = product.Price;

            await _context.SaveChangesAsync();

            // Log quantity change if it occurred
            if (oldQuantity != product.Quantity)
            {
                var log = new InventoryLog
                {
                    ProductId = id,
                    ProductName = product.Name,
                    OldQuantity = oldQuantity,
                    NewQuantity = product.Quantity,
                    TransactionType = product.Quantity > oldQuantity ? "Restock" : "Sale",
                    Notes = $"Quantity changed from {oldQuantity} to {product.Quantity}"
                };
                _context.InventoryLogs.Add(log);
                await _context.SaveChangesAsync();
            }

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}