using Microsoft.EntityFrameworkCore;
using Sts.Infrastructure.Data.Entities;

namespace Sts.Infrastructure.Data;

public sealed class StsDbContext(DbContextOptions<StsDbContext> options) : DbContext(options)
{
    public DbSet<SectionEntity> Sections { get; set; }
    public DbSet<PostEntity> Posts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SectionEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Title).IsRequired();
            e.Property(s => s.Order).IsRequired();
            e.HasMany(s => s.Posts)
             .WithOne(p => p.Section)
             .HasForeignKey(p => p.SectionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.SectionId).IsRequired();
            e.Property(p => p.Title).IsRequired();
            e.Property(p => p.Content).IsRequired();
        });
    }
}
