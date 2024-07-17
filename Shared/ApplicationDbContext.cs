using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;

namespace Shared;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public DbSet<AccessToken> AccessToken { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<OrderEntry> OrderEntry { get; set; }
        public DbSet<DeliveryAddress> DeliveryAddress { get; set; }
        public DbSet<Consignment> Consignment { get; set; }
        public DbSet<ConsignmentEntry> ConsignmentEntry { get; set; }
        public DbSet<ShippingAddress> ShippingAddress { get; set; }
        public DbSet<Return> Return { get; set; }
        public DbSet<ReturnEntry> ReturnEntry { get; set; }
        public DbSet<ReturnConsignment> ReturnConsignment { get; set; }
        public DbSet<ReturnPackage> ReturnPackage { get; set; }
        public DbSet<Address> Address { get; set; }
        public DbSet<CustomerInfo> CustomerInfo { get; set; }
        
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AccessToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ExpiresAt);
            });

            builder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Created).IsRequired();
                entity.Property(e => e.Modified).IsRequired();
                entity.Property(e => e.AldiCustomerNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EmailAddress).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(30);
                entity.Property(e => e.Language).HasMaxLength(2);
                entity.Property(e => e.OrderDeliveryArea).HasMaxLength(1);
                entity.Property(e => e.Exported)
                    .IsRequired()
                    .HasDefaultValue(false);
            });

            builder.Entity<OrderEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntryNumber).IsRequired();
                entity.Property(e => e.VendorProductCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AldiProductCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.CanceledOrReturnedQuantity).IsRequired();
                entity.Property(e => e.CarrierCode).HasMaxLength(50);
                entity.Property(e => e.AldiSuedProductCode).HasMaxLength(50);

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.Entries)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                
                entity.HasOne(e => e.DeliveryAddress)
                    .WithOne(d => d.OrderEntry)
                    .HasForeignKey<OrderEntry>(e => e.DeliveryAddressId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<DeliveryAddress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(30);
                entity.Property(e => e.SalutationCode).IsRequired().HasMaxLength(30);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.StreetName).HasMaxLength(100);
                entity.Property(e => e.StreetNumber).HasMaxLength(100);
                entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Town).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CountryIsoCode).IsRequired().HasMaxLength(3);
                entity.Property(e => e.Remarks).HasMaxLength(200);
                entity.Property(e => e.PackstationNumber).HasMaxLength(30);
                entity.Property(e => e.PostNumber).HasMaxLength(10);
                entity.Property(e => e.PostOfficeNumber).HasMaxLength(10);
            });

            builder.Entity<Consignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.VendorConsignmentCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.StatusText).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TrackingId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TrackingLink).HasMaxLength(100);
                entity.Property(e => e.Carrier).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ShippingDate).IsRequired();
                entity.Property(e => e.ExpectedDelivery);
                entity.Property(e => e.ReceiptDelivery);
                entity.Property(e => e.AldiConsignmentCode).HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OrderCode).IsRequired();
                
                entity.HasOne(c => c.Order) 
                    .WithMany(o => o.Consignments)
                    .HasForeignKey(c => c.OrderCode)
                    .HasPrincipalKey(o => o.Code)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(c => c.ShippingAddress)
                    .WithOne()
                    .HasForeignKey<Consignment>(c => c.ShippingAddressId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            builder.Entity<ConsignmentEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderEntryNumber).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.CancelledOrReturnedQuantity).IsRequired();

                entity.HasOne(e => e.Consignment)
                    .WithMany(c => c.ConsignmentEntries)
                    .HasForeignKey(e => e.ConsignmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.OrderEntry)
                    .WithMany(oe => oe.ConsignmentEntries)
                    .HasForeignKey(e => e.OrderEntryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.OrderEntryNumber).IsRequired();
            });

            builder.Entity<ShippingAddress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(30);
                entity.Property(e => e.SalutationCode).IsRequired().HasMaxLength(30);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.StreetName).HasMaxLength(100);
                entity.Property(e => e.StreetNumber).HasMaxLength(100);
                entity.Property(e => e.Remarks).HasMaxLength(200);
                entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Town).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PackstationNumber).HasMaxLength(30);
                entity.Property(e => e.PostNumber).HasMaxLength(10);
                entity.Property(e => e.PostOfficeNumber).HasMaxLength(10);
                entity.Property(e => e.CountryIsoCode).IsRequired().HasMaxLength(3);
            });

            builder.Entity<Return>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderCode).IsRequired().HasMaxLength(100);
                entity.Property(e => e.InitiationDate).IsRequired();
                entity.Property(e => e.AldiReturnCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Rma).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);


                builder.Entity<Return>()
                    .HasOne(r => r.CustomerInfo)
                    .WithMany(ci => ci.Returns)
                    .HasForeignKey(r => r.CustomerInfoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(r => r.ReturnEntries)
                    .WithOne(re => re.Return)
                    .HasForeignKey(re => re.ReturnId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Order)
                    .WithMany(o => o.Returns)
                    .HasForeignKey(r => r.OrderCode)
                    .HasPrincipalKey(o => o.Code)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            builder.Entity<ReturnEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reason).HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.CanceledOrReturnedQuantity).IsRequired();
                entity.Property(e => e.OrderEntryNumber).IsRequired();
                entity.Property(e => e.EntryCode).HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CarrierCode).HasMaxLength(100);

                entity.HasMany(re => re.ReturnConsignments)
                    .WithOne(c => c.ReturnEntry)
                    .HasForeignKey(c => c.ReturnEntryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ReturnConsignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ConsignmentCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.CanceledQuantity).IsRequired();
                entity.Property(e => e.CompletedQuantity).IsRequired();
                entity.Property(e => e.Carrier).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CarrierCode).HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CompletedDate);


                entity.HasMany(rc => rc.Packages)
                    .WithOne(p => p.ReturnConsignment)
                    .HasForeignKey(p => p.ReturnConsignmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ReturnPackage>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.VendorPackageCode).IsRequired().HasMaxLength(100);
                entity.Property(a => a.TrackingId).IsRequired().HasMaxLength(50);
                entity.Property(a => a.TrackingLink).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(a => a.ReceiptDelivery);
            });

            builder.Entity<CustomerInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EmailAddress).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(50);

                entity.HasOne(ci => ci.Address)
                    .WithOne()
                    .HasForeignKey<CustomerInfo>(ci => ci.AddressId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Address>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Type).IsRequired().HasMaxLength(50);
                entity.Property(a => a.SalutationCode).HasMaxLength(10);
                entity.Property(a => a.FirstName).IsRequired().HasMaxLength(150);
                entity.Property(a => a.LastName).IsRequired().HasMaxLength(150);
                entity.Property(a => a.StreetName).HasMaxLength(100);
                entity.Property(a => a.StreetNumber).HasMaxLength(100);
                entity.Property(a => a.Remarks).HasMaxLength(500);
                entity.Property(a => a.PostalCode).IsRequired().HasMaxLength(10);
                entity.Property(a => a.Town).IsRequired().HasMaxLength(100);
                entity.Property(a => a.PackstationNumber).HasMaxLength(20);
                entity.Property(a => a.PostNumber).HasMaxLength(20);
                entity.Property(a => a.PostOfficeNumber).HasMaxLength(20);
                entity.Property(a => a.CountryIsoCode).IsRequired().HasMaxLength(3);
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = Environment.GetEnvironmentVariable("MAGMA_ALDI_CONNECTIONSTRING_TEST");

                if (!string.IsNullOrEmpty(connectionString))
                {
                    optionsBuilder.UseSqlServer(connectionString);
                }
                else
                {
                    throw new InvalidOperationException("Keine gültige Verbindungszeichenfolge in der Umgebungsvariablen gefunden.");
                }
            }
        }
    }