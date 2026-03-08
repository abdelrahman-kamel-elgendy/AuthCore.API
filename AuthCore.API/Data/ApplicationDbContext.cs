using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AuthCore.API.Models;

namespace AuthCore.API.Data;

public class ApplicationDbContext : IdentityDbContext<User> {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);

        // Make Email unique
        builder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
    }
}