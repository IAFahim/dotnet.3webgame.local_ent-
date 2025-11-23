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

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("idx_applicationuser_email");
            
            entity.HasIndex(u => u.UserName)
                .IsUnique()
                .HasDatabaseName("idx_applicationuser_username");
            
            entity.HasIndex(u => u.LastLoginAt)
                .HasDatabaseName("idx_applicationuser_lastlogin");

            entity.OwnsMany(u => u.RefreshTokens, rt =>
            {
                rt.WithOwner().HasForeignKey("UserId");
                rt.ToTable("refresh_tokens");
                rt.HasKey(t => t.Id);
                rt.Property(t => t.Id).ValueGeneratedOnAdd();
                
                rt.HasIndex(t => t.Token)
                    .IsUnique()
                    .HasDatabaseName("idx_refreshtoken_token");
                
                rt.HasIndex(t => t.Expires)
                    .HasDatabaseName("idx_refreshtoken_expires");
                
                rt.HasIndex(t => t.Revoked)
                    .HasDatabaseName("idx_refreshtoken_revoked");
            });
        });
    }
}
