using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventorySystem.Data;
using InventorySystem.Models;

namespace InventoryBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SalesController> _logger;

        public SalesController(AppDbContext context, ILogger<SalesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/sales
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sale>>> GetAllSales()
        {
            try
            {
                var sales = await _context.Sales.OrderByDescending(s => s.SaleDate).ToListAsync();
                return Ok(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving sales: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving sales" });
            }
        }

        // GET: api/sales/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Sale>> GetSaleById(int id)
        {
            try
            {
                var sale = await _context.Sales.FindAsync(id);
                if (sale == null)
                {
                    return NotFound(new { message = "Sale not found" });
                }
                return Ok(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving sale: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving sale" });
            }
        }

        // GET: api/sales/profit/summary
        [HttpGet("profit/summary")]
        public async Task<ActionResult<object>> GetProfitSummary()
        {
            try
            {
                var sales = await _context.Sales.ToListAsync();
                var totalRevenue = sales.Sum(s => s.SellingPrice * s.QuantitySold);
                var totalCost = sales.Sum(s => s.CostPrice * s.QuantitySold);
                var totalProfit = sales.Sum(s => s.TotalProfit);
                var totalQuantitySold = sales.Sum(s => s.QuantitySold);

                return Ok(new
                {
                    totalSales = sales.Count,
                    totalQuantitySold = totalQuantitySold,
                    totalRevenue = totalRevenue,
                    totalCost = totalCost,
                    totalProfit = totalProfit,
                    averageProfitPerSale = sales.Count > 0 ? totalProfit / sales.Count : 0,
                    profitMargin = totalRevenue > 0 ? (totalProfit / totalRevenue) * 100 : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating profit summary: {ex.Message}");
                return StatusCode(500, new { message = "Error calculating profit summary" });
            }
        }

        // POST: api/sales
        [HttpPost]
        public async Task<ActionResult<Sale>> CreateSale([FromBody] CreateSaleRequest request)
        {
            try
            {
                // Validate input
                if (request.QuantitySold <= 0)
                {
                    return BadRequest(new { message = "Quantity must be greater than 0" });
                }

                if (request.SellingPrice < 0 || request.CostPrice < 0)
                {
                    return BadRequest(new { message = "Prices cannot be negative" });
                }

                // Get product
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Check inventory
                if (product.Quantity < request.QuantitySold)
                {
                    return BadRequest(new { message = $"Insufficient inventory. Available: {product.Quantity}, Requested: {request.QuantitySold}" });
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Create sale record
                        var sale = new Sale
                        {
                            ProductId = request.ProductId,
                            ProductName = product.Name,
                            QuantitySold = request.QuantitySold,
                            SellingPrice = request.SellingPrice,
                            CostPrice = request.CostPrice,
                            Notes = request.Notes
                        };

                        _context.Sales.Add(sale);

                        // Deduct from inventory
                        int oldQuantity = product.Quantity;
                        product.Quantity -= request.QuantitySold;
                        _context.Products.Update(product);

                        // Create inventory log
                        var log = new InventoryLog
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            OldQuantity = oldQuantity,
                            NewQuantity = product.Quantity,
                            TransactionType = "Sale",
                            Notes = $"Sold {request.QuantitySold} units at {request.SellingPrice} per unit. {request.Notes}"
                        };

                        _context.InventoryLogs.Add(log);

                        // Save all changes
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Sale created successfully. Product: {product.Name}, Quantity: {request.QuantitySold}");

                        return CreatedAtAction(nameof(GetSaleById), new { id = sale.Id }, sale);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError($"Error in transaction: {ex.Message}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating sale: {ex.Message}");
                return StatusCode(500, new { message = "Error creating sale" });
            }
        }

        // DELETE: api/sales/{id} (optional - for reversing sales)
        [HttpDelete("{id}")]
        public async Task<IActionResult> ReverseSale(int id)
        {
            try
            {
                var sale = await _context.Sales.FindAsync(id);
                if (sale == null)
                {
                    return NotFound(new { message = "Sale not found" });
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Get product and restore inventory
                        var product = await _context.Products.FindAsync(sale.ProductId);
                        if (product != null)
                        {
                            int oldQuantity = product.Quantity;
                            product.Quantity += sale.QuantitySold;
                            _context.Products.Update(product);

                            // Create log for reversal
                            var log = new InventoryLog
                            {
                                ProductId = product.Id,
                                ProductName = product.Name,
                                OldQuantity = oldQuantity,
                                NewQuantity = product.Quantity,
                                TransactionType = "Sale Reversal",
                                Notes = $"Reversed sale of {sale.QuantitySold} units"
                            };

                            _context.InventoryLogs.Add(log);
                        }

                        _context.Sales.Remove(sale);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Sale reversed successfully. Sale ID: {id}");

                        return Ok(new { message = "Sale reversed successfully" });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError($"Error in transaction: {ex.Message}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reversing sale: {ex.Message}");
                return StatusCode(500, new { message = "Error reversing sale" });
            }
        }
    }

    public class CreateSaleRequest
    {
        public int ProductId { get; set; }
        public int QuantitySold { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal CostPrice { get; set; }
        public string? Notes { get; set; }
    }
}
