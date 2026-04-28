using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IInvoiceService _invoiceService;

        public DashboardController(ApplicationDbContext context, IInvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var today = DateTime.UtcNow.Date;

            var dto = new AdminDashboardDto
            {
                TotalParts = await _context.Parts.CountAsync(p => p.IsActive),
                LowStockParts = await _context.Parts.CountAsync(p => p.IsActive && p.Stock < p.MinStockLevel),
                TotalStaff = await _context.Users.CountAsync(u => u.Role == "Staff" && u.IsActive),
                TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer" && u.IsActive),
                TotalVendors = await _context.Vendors.CountAsync(v => v.IsActive),
                TotalRevenue = await _context.SalesInvoices.SumAsync(s => s.FinalAmount),
                TodayRevenue = await _context.SalesInvoices.Where(s => s.Date.Date == today).SumAsync(s => s.FinalAmount),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == "Pending"),
                UnreadNotifications = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
                LowStockPartsList = await _context.Parts.Include(p => p.Vendor).Where(p => p.IsActive && p.Stock < p.MinStockLevel)
                    .Select(p => new PartResponseDto { Id = p.Id, Name = p.Name, Stock = p.Stock, MinStockLevel = p.MinStockLevel, SellingPrice = p.SellingPrice, Category = p.Category, VendorName = p.Vendor != null ? p.Vendor.CompanyName : null, IsActive = p.IsActive, CreatedAt = p.CreatedAt, CostPrice = p.CostPrice, VendorId = p.VendorId })
                    .ToListAsync()
            };

            return Ok(ApiResponse<AdminDashboardDto>.Ok(dto));
        }

        [HttpGet("staff")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> StaffDashboard()
        {
            var today = DateTime.UtcNow.Date;
            var staffId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var dto = new StaffDashboardDto
            {
                TodaySalesCount = await _context.SalesInvoices.CountAsync(s => s.StaffId == staffId && s.Date.Date == today),
                TodaySalesAmount = await _context.SalesInvoices.Where(s => s.StaffId == staffId && s.Date.Date == today).SumAsync(s => s.FinalAmount),
                TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer" && u.IsActive),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == "Pending"),
                RecentSales = await _context.SalesInvoices.Include(s => s.Customer).Include(s => s.Staff).Include(s => s.Items).ThenInclude(i => i.Part)
                    .Where(s => s.StaffId == staffId).OrderByDescending(s => s.Date).Take(5)
                    .Select(s => new SalesInvoiceResponseDto { Id = s.Id, CustomerId = s.CustomerId, CustomerName = s.Customer.FullName, StaffId = s.StaffId, TotalAmount = s.TotalAmount, Discount = s.Discount, FinalAmount = s.FinalAmount, PaymentMethod = s.PaymentMethod, PaymentStatus = s.PaymentStatus, Date = s.Date, LoyaltyApplied = s.Discount > 0 })
                    .ToListAsync()
            };

            return Ok(ApiResponse<StaffDashboardDto>.Ok(dto));
        }

        [HttpGet("customer")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CustomerDashboard()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var dto = new CustomerDashboardDto
            {
                TotalPurchases = await _context.SalesInvoices.CountAsync(s => s.CustomerId == userId),
                TotalSpent = await _context.SalesInvoices.Where(s => s.CustomerId == userId).SumAsync(s => s.FinalAmount),
                VehicleCount = await _context.Vehicles.CountAsync(v => v.CustomerId == userId),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.CustomerId == userId && a.Status == "Pending"),
                PendingPartRequests = await _context.PartRequests.CountAsync(pr => pr.CustomerId == userId && pr.Status == "Pending"),
                RecentPurchases = await _context.SalesInvoices.Include(s => s.Staff).Include(s => s.Items).ThenInclude(i => i.Part)
                    .Where(s => s.CustomerId == userId).OrderByDescending(s => s.Date).Take(5)
                    .Select(s => new SalesInvoiceResponseDto { Id = s.Id, StaffName = s.Staff.FullName, TotalAmount = s.TotalAmount, Discount = s.Discount, FinalAmount = s.FinalAmount, PaymentMethod = s.PaymentMethod, PaymentStatus = s.PaymentStatus, Date = s.Date, LoyaltyApplied = s.Discount > 0 })
                    .ToListAsync(),
                UpcomingAppointments = await _context.Appointments.Where(a => a.CustomerId == userId && a.ScheduledDate >= DateTime.UtcNow).OrderBy(a => a.ScheduledDate).Take(5)
                    .Select(a => new AppointmentResponseDto { Id = a.Id, ScheduledDate = a.ScheduledDate, ServiceType = a.ServiceType, Status = a.Status, Notes = a.Notes, CreatedAt = a.CreatedAt })
                    .ToListAsync()
            };

            return Ok(ApiResponse<CustomerDashboardDto>.Ok(dto));
        }
    }
}
