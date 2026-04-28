using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    /// <summary>
    /// Represents a sales invoice (parts sold to a customer).
    /// </summary>
    public class SalesInvoice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int StaffId { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Discount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal FinalAmount { get; set; }

        [Required]
        [MaxLength(30)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Credit, Card

        [Required]
        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Paid"; // Paid, Credit

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("CustomerId")]
        public User Customer { get; set; } = null!;

        [ForeignKey("StaffId")]
        public User Staff { get; set; } = null!;

        public ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
    }
}
