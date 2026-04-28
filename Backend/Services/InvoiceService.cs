using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Services
{
    /// <summary>
    /// Handles purchase and sales invoice creation with stock management and loyalty discount.
    /// </summary>
    public interface IInvoiceService
    {
        // Purchase Invoices
        Task<List<PurchaseInvoiceResponseDto>> GetPurchaseInvoicesAsync();
        Task<PurchaseInvoiceResponseDto?> GetPurchaseInvoiceByIdAsync(int id);
        Task<PurchaseInvoiceResponseDto> CreatePurchaseInvoiceAsync(PurchaseInvoiceCreateDto dto, int createdById);

        // Sales Invoices
        Task<List<SalesInvoiceResponseDto>> GetSalesInvoicesAsync();
        Task<SalesInvoiceResponseDto?> GetSalesInvoiceByIdAsync(int id);
        Task<SalesInvoiceResponseDto> CreateSalesInvoiceAsync(SalesInvoiceCreateDto dto, int staffId);
        Task<List<SalesInvoiceResponseDto>> GetCustomerInvoicesAsync(int customerId);
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPartService _partService;

        // Loyalty discount threshold and percentage
        private const decimal LoyaltyThreshold = 5000m;
        private const decimal LoyaltyDiscountPercent = 10m;

        public InvoiceService(ApplicationDbContext context, IPartService partService)
        {
            _context = context;
            _partService = partService;
        }

        // ─── Purchase Invoices ─────────────────────────────────────────

        public async Task<List<PurchaseInvoiceResponseDto>> GetPurchaseInvoicesAsync()
        {
            return await _context.PurchaseInvoices
                .Include(pi => pi.Vendor)
                .Include(pi => pi.CreatedBy)
                .Include(pi => pi.Items).ThenInclude(i => i.Part)
                .OrderByDescending(pi => pi.Date)
                .Select(pi => MapPurchaseInvoice(pi))
                .ToListAsync();
        }

        public async Task<PurchaseInvoiceResponseDto?> GetPurchaseInvoiceByIdAsync(int id)
        {
            var pi = await _context.PurchaseInvoices
                .Include(p => p.Vendor)
                .Include(p => p.CreatedBy)
                .Include(p => p.Items).ThenInclude(i => i.Part)
                .FirstOrDefaultAsync(p => p.Id == id);

            return pi == null ? null : MapPurchaseInvoice(pi);
        }

        /// <summary>
        /// Create a purchase invoice from vendor: adds items and updates part stock.
        /// </summary>
        public async Task<PurchaseInvoiceResponseDto> CreatePurchaseInvoiceAsync(
            PurchaseInvoiceCreateDto dto, int createdById)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var invoice = new PurchaseInvoice
                {
                    VendorId = dto.VendorId,
                    Notes = dto.Notes,
                    CreatedById = createdById,
                    Date = DateTime.UtcNow,
                    TotalAmount = 0
                };

                _context.PurchaseInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                decimal total = 0;

                foreach (var itemDto in dto.Items)
                {
                    var item = new PurchaseInvoiceItem
                    {
                        PurchaseInvoiceId = invoice.Id,
                        PartId = itemDto.PartId,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice
                    };

                    _context.PurchaseInvoiceItems.Add(item);
                    total += itemDto.Quantity * itemDto.UnitPrice;

                    // Update part stock (stock in)
                    var part = await _context.Parts.FindAsync(itemDto.PartId);
                    if (part != null)
                    {
                        part.Stock += itemDto.Quantity;
                        part.UpdatedAt = DateTime.UtcNow;
                    }
                }

                invoice.TotalAmount = total;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (await GetPurchaseInvoiceByIdAsync(invoice.Id))!;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─── Sales Invoices ────────────────────────────────────────────

        public async Task<List<SalesInvoiceResponseDto>> GetSalesInvoicesAsync()
        {
            return await _context.SalesInvoices
                .Include(si => si.Customer)
                .Include(si => si.Staff)
                .Include(si => si.Items).ThenInclude(i => i.Part)
                .OrderByDescending(si => si.Date)
                .Select(si => MapSalesInvoice(si))
                .ToListAsync();
        }

        public async Task<SalesInvoiceResponseDto?> GetSalesInvoiceByIdAsync(int id)
        {
            var si = await _context.SalesInvoices
                .Include(s => s.Customer)
                .Include(s => s.Staff)
                .Include(s => s.Items).ThenInclude(i => i.Part)
                .FirstOrDefaultAsync(s => s.Id == id);

            return si == null ? null : MapSalesInvoice(si);
        }

        /// <summary>
        /// Create a sales invoice: validates stock, applies loyalty discount (>5000),
        /// deducts stock, and generates low-stock notifications.
        /// </summary>
        public async Task<SalesInvoiceResponseDto> CreateSalesInvoiceAsync(
            SalesInvoiceCreateDto dto, int staffId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get admin user for low-stock notifications
                var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");

                var invoice = new SalesInvoice
                {
                    CustomerId = dto.CustomerId,
                    StaffId = staffId,
                    PaymentMethod = dto.PaymentMethod,
                    PaymentStatus = dto.PaymentStatus,
                    Date = DateTime.UtcNow,
                    TotalAmount = 0,
                    Discount = 0,
                    FinalAmount = 0
                };

                _context.SalesInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                decimal total = 0;

                foreach (var itemDto in dto.Items)
                {
                    var part = await _context.Parts.FindAsync(itemDto.PartId);
                    if (part == null || !part.IsActive)
                        throw new InvalidOperationException($"Part with ID {itemDto.PartId} not found.");

                    if (part.Stock < itemDto.Quantity)
                        throw new InvalidOperationException(
                            $"Insufficient stock for \"{part.Name}\". Available: {part.Stock}, Requested: {itemDto.Quantity}");

                    var item = new SalesInvoiceItem
                    {
                        SalesInvoiceId = invoice.Id,
                        PartId = itemDto.PartId,
                        Quantity = itemDto.Quantity,
                        UnitPrice = part.SellingPrice
                    };

                    _context.SalesInvoiceItems.Add(item);
                    total += itemDto.Quantity * part.SellingPrice;

                    // Deduct stock
                    part.Stock -= itemDto.Quantity;
                    part.UpdatedAt = DateTime.UtcNow;
                }

                // Apply loyalty discount: 10% off if total > 5000
                decimal discount = 0;
                if (total > LoyaltyThreshold)
                {
                    discount = Math.Round(total * LoyaltyDiscountPercent / 100, 2);
                }

                invoice.TotalAmount = total;
                invoice.Discount = discount;
                invoice.FinalAmount = total - discount;

                await _context.SaveChangesAsync();

                // Check low stock for all sold parts
                if (admin != null)
                {
                    foreach (var itemDto in dto.Items)
                    {
                        await _partService.CheckLowStockAsync(itemDto.PartId, admin.Id);
                    }
                }

                await transaction.CommitAsync();

                return (await GetSalesInvoiceByIdAsync(invoice.Id))!;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<SalesInvoiceResponseDto>> GetCustomerInvoicesAsync(int customerId)
        {
            return await _context.SalesInvoices
                .Include(si => si.Customer)
                .Include(si => si.Staff)
                .Include(si => si.Items).ThenInclude(i => i.Part)
                .Where(si => si.CustomerId == customerId)
                .OrderByDescending(si => si.Date)
                .Select(si => MapSalesInvoice(si))
                .ToListAsync();
        }

        // ─── Mapping Helpers ──────────────────────────────────────────

        private static PurchaseInvoiceResponseDto MapPurchaseInvoice(PurchaseInvoice pi)
        {
            return new PurchaseInvoiceResponseDto
            {
                Id = pi.Id,
                VendorId = pi.VendorId,
                VendorName = pi.Vendor?.CompanyName ?? "",
                TotalAmount = pi.TotalAmount,
                Date = pi.Date,
                Notes = pi.Notes,
                CreatedBy = pi.CreatedBy?.FullName ?? "",
                Items = pi.Items.Select(i => new PurchaseInvoiceItemResponseDto
                {
                    Id = i.Id,
                    PartId = i.PartId,
                    PartName = i.Part?.Name ?? "",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.Quantity * i.UnitPrice
                }).ToList()
            };
        }

        private static SalesInvoiceResponseDto MapSalesInvoice(SalesInvoice si)
        {
            return new SalesInvoiceResponseDto
            {
                Id = si.Id,
                CustomerId = si.CustomerId,
                CustomerName = si.Customer?.FullName ?? "",
                CustomerEmail = si.Customer?.Email ?? "",
                StaffId = si.StaffId,
                StaffName = si.Staff?.FullName ?? "",
                TotalAmount = si.TotalAmount,
                Discount = si.Discount,
                FinalAmount = si.FinalAmount,
                PaymentMethod = si.PaymentMethod,
                PaymentStatus = si.PaymentStatus,
                Date = si.Date,
                LoyaltyApplied = si.Discount > 0,
                Items = si.Items.Select(i => new SalesInvoiceItemResponseDto
                {
                    Id = i.Id,
                    PartId = i.PartId,
                    PartName = i.Part?.Name ?? "",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.Quantity * i.UnitPrice
                }).ToList()
            };
        }
    }
}
