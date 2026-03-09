using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropertySalesTracker.Models;

namespace PropertySalesTracker.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<AccountOfficer> AccountOfficers { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertySale> PropertySales { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<PropertySale>()
                .HasOne(ps => ps.Customer)
                .WithMany(c => c.PropertySales)
                .HasForeignKey(ps => ps.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PropertySale>()
                .HasOne(ps => ps.Property)
                .WithMany(p => p.PropertySales)
                .HasForeignKey(ps => ps.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AccountOfficer>()
                .HasKey(a => a.UserId); 

            builder.Entity<AccountOfficer>()
                .HasOne(a => a.User)
                .WithOne()
                .HasForeignKey<AccountOfficer>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AccountOfficer>()
                .Property(a => a.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<AccountOfficer>()
                .Property(a => a.Email)
                .IsRequired()
                .HasMaxLength(100);
        }
    }
}
