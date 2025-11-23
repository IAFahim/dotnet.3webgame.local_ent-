using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rest.Models;

namespace Rest.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
    : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .OwnsMany(u => u.RefreshTokens, a =>
            {
                a.WithOwner().HasForeignKey("UserId");
                a.ToTable("refresh_tokens");
                a.HasKey(t => t.Id); // Explicitly define Key
                a.Property(t => t.Id).ValueGeneratedOnAdd();
            });
    }
}