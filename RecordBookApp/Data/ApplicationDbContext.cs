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

        modelBuilder.Entity<Category>().HasData(
               new Category { CategoryId = 1, Name = "Salary", Type = "CashIn" },
               new Category { CategoryId = 2, Name = "Bonus", Type = "CashIn" },
               new Category { CategoryId = 3, Name = "Income from Rent", Type = "CashIn" },
               new Category { CategoryId = 4, Name = "Businesss Profit", Type = "CashIn" },
               new Category { CategoryId = 5, Name = "Other", Type = "CashIn" },
               new Category { CategoryId = 6, Name = "Stock Market", Type = "CashOut" },
               new Category { CategoryId = 7, Name = "Grocery", Type = "CashOut" },
               new Category { CategoryId = 8, Name = "Bills", Type = "CashOut" },
               new Category { CategoryId = 9, Name = "Health", Type = "CashOut" },
               new Category { CategoryId = 10, Name = "Fuel", Type = "CashOut" },
               new Category { CategoryId = 11, Name = "Rent", Type = "CashOut" },
               new Category { CategoryId = 12, Name = "Maintenance", Type = "CashOut" },
               new Category { CategoryId = 13, Name = "Labour Charges", Type = "CashOut" }
        );

        modelBuilder.Entity<Payment>().HasData(
                new Payment { PaymentId = 1, Type = "Online/UPI/Net Banking" },
                new Payment { PaymentId = 2, Type = "Offline/Cash" }
            );
    }
}
