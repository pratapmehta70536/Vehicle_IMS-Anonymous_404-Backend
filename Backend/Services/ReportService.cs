using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;

namespace Backend.Services
{
    public interface IReportService
    {
        Task<FinancialReportDto> GetFinancialReportAsync(string period, DateTime? startDate, DateTime? endDate);
        Task<CustomerReportDto> GetCustomerReportAsync();
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        public ReportService(ApplicationDbContext context) { _context = context; }

        public async Task<FinancialReportDto> GetFinancialReportAsync(string period, DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.UtcNow.AddYears(-1);
            var end = endDate ?? DateTime.UtcNow;
            var sales = await _context.SalesInvoices.Where(s => s.Date >= start && s.Date <= end).ToListAsync();
            var purchases = await _context.PurchaseInvoices.Where(p => p.Date >= start && p.Date <= end).ToListAsync();
            var totalSales = sales.Sum(s => s.FinalAmount);
            var totalPurchases = purchases.Sum(p => p.TotalAmount);
            var breakdown = new List<FinancialReportItemDto>();

            if (period.Equals("daily", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 29; i >= 0; i--)
                {
                    var day = DateTime.UtcNow.Date.AddDays(-i);
                    var ds = sales.Where(s => s.Date.Date == day).Sum(s => s.FinalAmount);
                    var dp = purchases.Where(p => p.Date.Date == day).Sum(p => p.TotalAmount);
                    breakdown.Add(new FinancialReportItemDto { Label = day.ToString("MMM dd"), Sales = ds, Purchases = dp, Profit = ds - dp, InvoiceCount = sales.Count(s => s.Date.Date == day) });
                }
            }
            else if (period.Equals("monthly", StringComparison.OrdinalIgnoreCase))
            {
                var months = sales.Select(s => new { s.Date.Year, s.Date.Month }).Union(purchases.Select(p => new { p.Date.Year, p.Date.Month })).Distinct().OrderBy(m => m.Year).ThenBy(m => m.Month);
                foreach (var m in months)
                {
                    var ms = sales.Where(s => s.Date.Year == m.Year && s.Date.Month == m.Month).Sum(s => s.FinalAmount);
                    var mp = purchases.Where(p => p.Date.Year == m.Year && p.Date.Month == m.Month).Sum(p => p.TotalAmount);
                    breakdown.Add(new FinancialReportItemDto { Label = new DateTime(m.Year, m.Month, 1).ToString("MMM yyyy"), Sales = ms, Purchases = mp, Profit = ms - mp, InvoiceCount = sales.Count(s => s.Date.Year == m.Year && s.Date.Month == m.Month) });
                }
            }
            else
            {
                var years = sales.Select(s => s.Date.Year).Union(purchases.Select(p => p.Date.Year)).Distinct().OrderBy(y => y);
                foreach (var y in years)
                {
                    var ys = sales.Where(s => s.Date.Year == y).Sum(s => s.FinalAmount);
                    var yp = purchases.Where(p => p.Date.Year == y).Sum(p => p.TotalAmount);
                    breakdown.Add(new FinancialReportItemDto { Label = y.ToString(), Sales = ys, Purchases = yp, Profit = ys - yp, InvoiceCount = sales.Count(s => s.Date.Year == y) });
                }
            }

            return new FinancialReportDto { Period = period, TotalSales = totalSales, TotalPurchases = totalPurchases, Profit = totalSales - totalPurchases, InvoiceCount = sales.Count, Breakdown = breakdown };
        }

        public async Task<CustomerReportDto> GetCustomerReportAsync()
        {
            var topSpenders = await _context.SalesInvoices.Include(si => si.Customer)
                .GroupBy(si => si.CustomerId)
                .Select(g => new TopSpenderDto { CustomerId = g.Key, CustomerName = g.First().Customer.FullName, Email = g.First().Customer.Email, TotalSpent = g.Sum(si => si.FinalAmount), PurchaseCount = g.Count() })
                .OrderByDescending(t => t.TotalSpent).Take(20).ToListAsync();

            var regularCustomers = await _context.SalesInvoices.Include(si => si.Customer)
                .GroupBy(si => si.CustomerId)
                .Select(g => new RegularCustomerDto { CustomerId = g.Key, CustomerName = g.First().Customer.FullName, Email = g.First().Customer.Email, PurchaseCount = g.Count(), TotalSpent = g.Sum(si => si.FinalAmount), LastPurchase = g.Max(si => si.Date) })
                .OrderByDescending(r => r.PurchaseCount).Take(20).ToListAsync();

            var cutoff = DateTime.UtcNow.AddDays(-30);
            var overdueCredits = await _context.SalesInvoices.Include(si => si.Customer)
                .Where(si => si.PaymentStatus == "Credit" && si.Date < cutoff)
                .Select(si => new OverdueCreditDto { CustomerId = si.CustomerId, CustomerName = si.Customer.FullName, Email = si.Customer.Email, Phone = si.Customer.Phone, CreditAmount = si.FinalAmount, InvoiceDate = si.Date, DaysOverdue = (int)(DateTime.UtcNow - si.Date).TotalDays, InvoiceId = si.Id })
                .ToListAsync();

            return new CustomerReportDto { TopSpenders = topSpenders, RegularCustomers = regularCustomers, OverdueCredits = overdueCredits };
        }
    }
}
