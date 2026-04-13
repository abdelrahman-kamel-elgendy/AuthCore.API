using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AuthCore.API.Models;

namespace AuthCore.API.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<UserModel, IdentityRole, string>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserPhone> UserPhones => Set<UserPhone>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure User (IdentityUser) - Keep default table name for now
        builder.Entity<UserModel>(entity =>
        {
            // Keep default table name "AspNetUsers" for now
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.MiddleName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.ProfileURL).HasMaxLength(500);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(u => u.UpdatedAt).HasDefaultValueSql("NOW()");
        });

        // RefreshToken configuration
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(rt => rt.TokenId);
            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.HasIndex(rt => rt.UserId);
            entity.HasIndex(rt => rt.IsActive);

            entity.Property(rt => rt.Token).HasMaxLength(128).IsRequired();
            entity.Property(rt => rt.CreatedByIp).HasMaxLength(45);
            entity.Property(rt => rt.RevokedByIp).HasMaxLength(45);
            entity.Property(rt => rt.IsActive).HasDefaultValue(true);

            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rt => rt.ReplacedByToken)
                .WithMany()
                .HasForeignKey(rt => rt.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PasswordResetToken configuration
        builder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens");
            entity.HasKey(prt => prt.TokenId);
            entity.HasIndex(prt => prt.Token).IsUnique();
            entity.HasIndex(prt => prt.UserId);
            entity.HasIndex(prt => new { prt.UserId, prt.IsUsed, prt.ExpiryDate });

            entity.Property(prt => prt.Token).HasMaxLength(256).IsRequired();

            entity.HasOne(prt => prt.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(prt => prt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserPhone configuration
        builder.Entity<UserPhone>(entity =>
        {
            entity.ToTable("UserPhones");
            entity.HasKey(up => new { up.UserId, up.PhoneNumber });
            entity.HasIndex(up => new { up.UserId, up.IsPrimary });

            entity.Property(up => up.CountryCode).HasMaxLength(5).IsRequired();
            entity.Property(up => up.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(up => up.IsPrimary).HasDefaultValue(false);

            entity.HasOne(up => up.User)
                .WithMany(u => u.UserPhones)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserAddress configuration
        builder.Entity<UserAddress>(entity =>
        {
            entity.ToTable("UserAddresses");
            entity.HasKey(ua => ua.AddressId);
            entity.HasIndex(ua => ua.UserId);
            entity.HasIndex(ua => new { ua.UserId, ua.IsDefault });

            entity.Property(ua => ua.AddressLine1).HasMaxLength(200).IsRequired();
            entity.Property(ua => ua.City).HasMaxLength(100).IsRequired();
            entity.Property(ua => ua.State).HasMaxLength(100);
            entity.Property(ua => ua.Country).HasMaxLength(100).IsRequired();
            entity.Property(ua => ua.PostalCode).HasMaxLength(20);
            entity.Property(ua => ua.IsDefault).HasDefaultValue(false);

            entity.HasOne(ua => ua.User)
                .WithMany(u => u.UserAddresses)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}