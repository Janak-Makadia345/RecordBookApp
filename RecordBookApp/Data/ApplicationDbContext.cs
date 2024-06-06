using Microsoft.EntityFrameworkCore;
using RecordBookApp.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Record> Records { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure one-to-many relationships
        modelBuilder.Entity<User>()
            .HasMany(u => u.Books)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId);

        modelBuilder.Entity<Book>()
            .HasMany(b => b.Records)
            .WithOne(r => r.Book)
            .HasForeignKey(r => r.BookId);

        modelBuilder.Entity<Category>()
            .HasMany(c => c.Records)
            .WithOne(r => r.Category)
            .HasForeignKey(r => r.CategoryId);

        modelBuilder.Entity<Payment>()
            .HasMany(p => p.Records)
            .WithOne(r => r.Payment)
            .HasForeignKey(r => r.PaymentId);
    }
}
