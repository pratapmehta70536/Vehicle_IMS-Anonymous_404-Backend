using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data
{
    /// <summary>
    /// Application database context for Entity Framework Core.
    /// Configures all entity mappings and relationships.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet properties for all entities
        public DbSet<User> Users => Set<User>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Vendor> Vendors => Set<Vendor>();
        public DbSet<Part> Parts => Set<Part>();
        public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
        public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
        public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<PartRequest> PartRequests => Set<PartRequest>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── User Configuration ──────────────────────────────────────
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Role).HasDefaultValue("Customer");
                entity.Property(u => u.IsActive).HasDefaultValue(true);
            });

            // ── Vehicle Configuration ───────────────────────────────────
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasIndex(v => v.VehicleNumber);
                entity.HasOne(v => v.Customer)
                      .WithMany(u => u.Vehicles)
                      .HasForeignKey(v => v.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Vendor Configuration ────────────────────────────────────
            modelBuilder.Entity<Vendor>(entity =>
            {
                entity.HasIndex(v => v.CompanyName);
            });

            // ── Part Configuration ──────────────────────────────────────
            modelBuilder.Entity<Part>(entity =>
            {
                entity.HasIndex(p => p.Name);
                entity.HasIndex(p => p.Category);
                entity.HasOne(p => p.Vendor)
                      .WithMany(v => v.Parts)
                      .HasForeignKey(p => p.VendorId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── PurchaseInvoice Configuration ───────────────────────────
            modelBuilder.Entity<PurchaseInvoice>(entity =>
            {
                entity.HasOne(pi => pi.Vendor)
                      .WithMany(v => v.PurchaseInvoices)
                      .HasForeignKey(pi => pi.VendorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pi => pi.CreatedBy)
                      .WithMany()
                      .HasForeignKey(pi => pi.CreatedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── PurchaseInvoiceItem Configuration ───────────────────────
            modelBuilder.Entity<PurchaseInvoiceItem>(entity =>
            {
                entity.Ignore(pii => pii.TotalPrice); // Computed property

                entity.HasOne(pii => pii.PurchaseInvoice)
                      .WithMany(pi => pi.Items)
                      .HasForeignKey(pii => pii.PurchaseInvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pii => pii.Part)
                      .WithMany(p => p.PurchaseInvoiceItems)
                      .HasForeignKey(pii => pii.PartId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── SalesInvoice Configuration ──────────────────────────────
            modelBuilder.Entity<SalesInvoice>(entity =>
            {
                entity.HasOne(si => si.Customer)
                      .WithMany(u => u.SalesAsCustomer)
                      .HasForeignKey(si => si.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(si => si.Staff)
                      .WithMany(u => u.SalesAsStaff)
                      .HasForeignKey(si => si.StaffId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── SalesInvoiceItem Configuration ──────────────────────────
            modelBuilder.Entity<SalesInvoiceItem>(entity =>
            {
                entity.Ignore(sii => sii.TotalPrice); // Computed property

                entity.HasOne(sii => sii.SalesInvoice)
                      .WithMany(si => si.Items)
                      .HasForeignKey(sii => sii.SalesInvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sii => sii.Part)
                      .WithMany(p => p.SalesInvoiceItems)
                      .HasForeignKey(sii => sii.PartId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Appointment Configuration ───────────────────────────────
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasOne(a => a.Customer)
                      .WithMany(u => u.Appointments)
                      .HasForeignKey(a => a.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── PartRequest Configuration ───────────────────────────────
            modelBuilder.Entity<PartRequest>(entity =>
            {
                entity.HasOne(pr => pr.Customer)
                      .WithMany(u => u.PartRequests)
                      .HasForeignKey(pr => pr.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Review Configuration ────────────────────────────────────
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.Customer)
                      .WithMany(u => u.Reviews)
                      .HasForeignKey(r => r.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Notification Configuration ──────────────────────────────
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => new { n.UserId, n.IsRead });
                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
