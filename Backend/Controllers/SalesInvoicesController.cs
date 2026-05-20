using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SalesInvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IEmailService _emailService;

        public SalesInvoicesController(IInvoiceService invoiceService, IEmailService emailService)
        {
            _invoiceService = invoiceService;
            _emailService = emailService;
        }

        [HttpGet]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetAll()
        {
            var invoices = await _invoiceService.GetSalesInvoicesAsync();
            return Ok(ApiResponse<List<SalesInvoiceResponseDto>>.Ok(invoices));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _invoiceService.GetSalesInvoiceByIdAsync(id);
            if (invoice == null) return NotFound(ApiResponse<object>.Fail("Invoice not found."));
            return Ok(ApiResponse<SalesInvoiceResponseDto>.Ok(invoice));
        }

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMy()
        {
            var customerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var invoices = await _invoiceService.GetCustomerInvoicesAsync(customerId);
            return Ok(ApiResponse<object>.Ok(invoices));
        }

        [HttpPost]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Create([FromBody] SalesInvoiceCreateDto dto)
        {
            try
            {
                var staffId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var invoice = await _invoiceService.CreateSalesInvoiceAsync(dto, staffId);
                return Ok(ApiResponse<SalesInvoiceResponseDto>.Ok(invoice, invoice.LoyaltyApplied ? $"Invoice created. 10% loyalty discount applied! Saved Rs. {invoice.Discount:N2}" : "Invoice created."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Update(int id, [FromBody] SalesInvoiceUpdateDto dto)
        {
            try
            {
                var staffId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var invoice = await _invoiceService.UpdateSalesInvoiceAsync(id, dto, staffId);
                return Ok(ApiResponse<SalesInvoiceResponseDto>.Ok(invoice, "Invoice updated successfully."));
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "Invoice not found.") return NotFound(ApiResponse<object>.Fail(ex.Message));
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _invoiceService.DeleteSalesInvoiceAsync(id);
            if (!success) return NotFound(ApiResponse<object>.Fail("Invoice not found."));
            return Ok(ApiResponse<object>.Ok(new { }, "Invoice deleted and parts restocked successfully."));
        }

        [HttpPost("{id}/pay")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PayOnline(int id)
        {
            try
            {
                var customerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _invoiceService.PayInvoiceOnlineAsync(id, customerId);
                return Ok(ApiResponse<object>.Ok(new { }, "Payment successful."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPost("{id}/refund")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Refund(int id)
        {
            try
            {
                await _invoiceService.RefundSalesInvoiceAsync(id);
                return Ok(ApiResponse<object>.Ok(new { }, "Invoice refunded and parts restocked successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPost("{id}/send-email")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> SendEmail(int id, [FromBody] SendInvoiceEmailDto dto)
        {
            var invoice = await _invoiceService.GetSalesInvoiceByIdAsync(id);
            if (invoice == null) return NotFound(ApiResponse<object>.Fail("Invoice not found."));

            var itemDetails = string.Join("", invoice.Items.Select(i => $"<p>{i.PartName} x{i.Quantity} = Rs. {i.TotalPrice:N2}</p>"));
            try
            {
                await _emailService.SendInvoiceEmailAsync(dto.ToEmail, invoice.CustomerName, invoice.FinalAmount, invoice.PaymentMethod, invoice.PaymentStatus, itemDetails);
                return Ok(ApiResponse<object>.Ok(new { }, "Invoice emailed successfully."));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Failed to send email. Check SMTP configuration."));
            }
        }
    }
}
