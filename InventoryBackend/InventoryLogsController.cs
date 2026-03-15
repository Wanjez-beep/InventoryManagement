using Microsoft.AspNetCore.Mvc;
using InventorySystem.Data;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryLogsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLogs()
        {
            var logs = await _context.InventoryLogs
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            return Ok(logs);
        }

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductLogs(int productId)
        {
            var logs = await _context.InventoryLogs
                .Where(l => l.ProductId == productId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            return Ok(logs);
        }

        [HttpGet("reconciliation-summary")]
        public async Task<IActionResult> GetReconciliationSummary()
        {
            var products = await _context.Products.ToListAsync();
            var summary = products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Category,
                ComputerQuantity = p.Quantity,
                LastModified = p.CreatedAt,
                RecentLogs = _context.InventoryLogs
                    .Where(l => l.ProductId == p.Id)
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(5)
                    .ToList()
            }).ToList();

            return Ok(summary);
        }

        [HttpGet("daterange")]
        public async Task<IActionResult> GetLogsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var logs = await _context.InventoryLogs
                .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            return Ok(logs);
        }
    }
}
