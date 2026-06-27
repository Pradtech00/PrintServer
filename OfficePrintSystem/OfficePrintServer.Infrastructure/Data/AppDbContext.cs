using Microsoft.EntityFrameworkCore;
using OfficePrintServer.Core.Entities;

namespace OfficePrintServer.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Printer> Printers { get; set; } = null!;
    public DbSet<PrintJob> PrintJobs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Printer>().HasKey(p => p.Id);
        modelBuilder.Entity<PrintJob>().HasKey(pj => pj.Id);
    }
}