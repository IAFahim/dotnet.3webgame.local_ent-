using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rest.Models;

namespace Rest.Data;

/// <summary>
/// Database context using ASP.NET Core Identity
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Customize Identity tables (optional)
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.CoinBalance).HasPrecision(18, 2);
        });
    }
}
