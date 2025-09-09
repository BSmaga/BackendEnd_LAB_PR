using Microsoft.EntityFrameworkCore;
using Biblioteka.Domain;

namespace Biblioteka.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ksiazka> Ksiazki => Set<Ksiazka>();
    public DbSet<Czytelnik> Czytelnicy => Set<Czytelnik>();
    public DbSet<Wypozyczenie> Wypozyczenia => Set<Wypozyczenie>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Czytelnik>()
            .HasIndex(x => x.Email)
            .IsUnique();

        b.Entity<Ksiazka>()
            .Property(x => x.Tytul).IsRequired();
        b.Entity<Ksiazka>()
            .Property(x => x.ISBN).IsRequired();

        b.Entity<Wypozyczenie>()
            .HasIndex(x => new { x.KsiazkaId, x.CzytelnikId, x.DataWypozyczenia });
    }
}