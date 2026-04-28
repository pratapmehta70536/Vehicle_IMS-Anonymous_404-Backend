using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    // ═══════════════════════════════════════════════════════════════
    //  Authentication DTOs
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Login request payload.</summary>
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Customer self-registration payload.</summary>
    public class RegisterDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }
    }

    /// <summary>Change password request (self-service, requires current password).</summary>
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>Admin-initiated password reset for a staff member (no current password needed).</summary>
    public class ResetPasswordDto
    {
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>Authentication response with JWT token.</summary>
    public class AuthResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════
    //  User / Staff DTOs
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Staff creation/update by admin.</summary>
    public class StaffDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }
    }

    /// <summary>Update staff info (without password).</summary>
    public class StaffUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>User response DTO (no password hash).</summary>
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Customer DTOs
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Customer registration by staff (includes vehicle).</summary>
    public class CustomerCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        // Vehicle details
        public VehicleDto? Vehicle { get; set; }
    }

    /// <summary>Customer profile update.</summary>
    public class CustomerUpdateDto
    {
        [MaxLength(100)]
        public string? FullName { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Vehicle DTOs
    // ═══════════════════════════════════════════════════════════════

    public class VehicleDto
    {
        [Required]
        [MaxLength(20)]
        public string VehicleNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Model { get; set; } = string.Empty;

        public int? Year { get; set; }

        [MaxLength(30)]
        public string? Color { get; set; }
    }

    public class VehicleResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int? Year { get; set; }
        public string? Color { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Vendor DTOs
    // ═══════════════════════════════════════════════════════════════

    public class VendorDto
    {
        [Required]
        [MaxLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ContactName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }
    }

    public class VendorResponseDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Part DTOs
    // ═══════════════════════════════════════════════════════════════

    public class PartDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [Required]
        public decimal CostPrice { get; set; }

        [Required]
        public decimal SellingPrice { get; set; }

        [Required]
        public int Stock { get; set; }

        public int MinStockLevel { get; set; } = 10;

        public int? VendorId { get; set; }
    }

    public class PartResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int Stock { get; set; }
        public int MinStockLevel { get; set; }
        public int? VendorId { get; set; }
        public string? VendorName { get; set; }
        public bool IsLowStock => Stock < MinStockLevel;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Purchase Invoice DTOs
    // ═══════════════════════════════════════════════════════════════

    public class PurchaseInvoiceCreateDto
    {
        [Required]
        public int VendorId { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public List<PurchaseInvoiceItemDto> Items { get; set; } = new();
    }

    public class PurchaseInvoiceItemDto
    {
        [Required]
        public int PartId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }
    }

    public class PurchaseInvoiceResponseDto
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<PurchaseInvoiceItemResponseDto> Items { get; set; } = new();
    }

    public class PurchaseInvoiceItemResponseDto
    {
        public int Id { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Sales Invoice DTOs
    // ═══════════════════════════════════════════════════════════════

    public class SalesInvoiceCreateDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "Cash";

        [Required]
        public string PaymentStatus { get; set; } = "Paid";

        [Required]
        public List<SalesInvoiceItemDto> Items { get; set; } = new();
    }

    public class SalesInvoiceItemDto
    {
        [Required]
        public int PartId { get; set; }

        [Required]
        public int Quantity { get; set; }
    }

    public class SalesInvoiceResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool LoyaltyApplied { get; set; }
        public List<SalesInvoiceItemResponseDto> Items { get; set; } = new();
    }

    public class SalesInvoiceItemResponseDto
    {
        public int Id { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Appointment DTOs
    // ═══════════════════════════════════════════════════════════════

    public class AppointmentDto
    {
        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [MaxLength(100)]
        public string ServiceType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class AppointmentUpdateDto
    {
        public DateTime? ScheduledDate { get; set; }
        public string? ServiceType { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }

    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Part Request DTOs
    // ═══════════════════════════════════════════════════════════════

    public class PartRequestDto
    {
        [Required]
        [MaxLength(150)]
        public string PartName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class PartRequestResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Review DTOs
    // ═══════════════════════════════════════════════════════════════

    public class ReviewDto
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Notification DTOs
    // ═══════════════════════════════════════════════════════════════

    public class NotificationResponseDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Report DTOs
    // ═══════════════════════════════════════════════════════════════

    public class FinancialReportDto
    {
        public string Period { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal Profit { get; set; }
        public int InvoiceCount { get; set; }
        public List<FinancialReportItemDto> Breakdown { get; set; } = new();
    }

    public class FinancialReportItemDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public decimal Purchases { get; set; }
        public decimal Profit { get; set; }
        public int InvoiceCount { get; set; }
    }

    public class CustomerReportDto
    {
        public List<TopSpenderDto> TopSpenders { get; set; } = new();
        public List<RegularCustomerDto> RegularCustomers { get; set; } = new();
        public List<OverdueCreditDto> OverdueCredits { get; set; } = new();
    }

    public class TopSpenderDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public decimal TotalSpent { get; set; }
        public int PurchaseCount { get; set; }
    }

    public class RegularCustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int PurchaseCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastPurchase { get; set; }
    }

    public class OverdueCreditDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public decimal CreditAmount { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int DaysOverdue { get; set; }
        public int InvoiceId { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Dashboard DTOs
    // ═══════════════════════════════════════════════════════════════

    public class AdminDashboardDto
    {
        public int TotalParts { get; set; }
        public int LowStockParts { get; set; }
        public int TotalStaff { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalVendors { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public int PendingAppointments { get; set; }
        public int UnreadNotifications { get; set; }
        public List<PartResponseDto> LowStockPartsList { get; set; } = new();
    }

    public class StaffDashboardDto
    {
        public int TodaySalesCount { get; set; }
        public decimal TodaySalesAmount { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingAppointments { get; set; }
        public List<SalesInvoiceResponseDto> RecentSales { get; set; } = new();
    }

    public class CustomerDashboardDto
    {
        public int TotalPurchases { get; set; }
        public decimal TotalSpent { get; set; }
        public int VehicleCount { get; set; }
        public int PendingAppointments { get; set; }
        public int PendingPartRequests { get; set; }
        public List<SalesInvoiceResponseDto> RecentPurchases { get; set; } = new();
        public List<AppointmentResponseDto> UpcomingAppointments { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Email DTO
    // ═══════════════════════════════════════════════════════════════

    public class SendInvoiceEmailDto
    {
        [Required]
        [EmailAddress]
        public string ToEmail { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Common / API Response Wrapper
    // ═══════════════════════════════════════════════════════════════

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success")
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message)
            => new() { Success = false, Message = message };
    }
}
