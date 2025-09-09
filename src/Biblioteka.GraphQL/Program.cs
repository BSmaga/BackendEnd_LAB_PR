using Biblioteka.Infrastructure;
using Biblioteka.Domain;

using HotChocolate;           
using HotChocolate.Data;      
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("db")));


builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

app.MapGraphQL("/graphql");
app.Run();

public class Query
{

    [UseFiltering]
    [UseSorting]
    public IQueryable<Ksiazka> GetKsiazki([Service] AppDbContext db)
        => db.Ksiazki.AsNoTracking();


    public Task<Ksiazka?> GetKsiazka([Service] AppDbContext db, int id)
        => db.Ksiazki.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id);
}

public record KsiazkaInput(
    string Tytul,
    string Autor,
    int Rok,
    string ISBN,
    int LiczbaEgzemplarzy
);

public class Mutation
{
    public async Task<Ksiazka> AddKsiazka([Service] AppDbContext db, KsiazkaInput input)
    {
        var ks = new Ksiazka
        {
            Tytul = input.Tytul,
            Autor = input.Autor,
            Rok = input.Rok,
            ISBN = input.ISBN,
            LiczbaEgzemplarzy = input.LiczbaEgzemplarzy
        };

        db.Ksiazki.Add(ks);
        await db.SaveChangesAsync();
        return ks;
    }

    public async Task<bool> DeleteKsiazka([Service] AppDbContext db, int id)
    {
        var ks = await db.Ksiazki.FindAsync(id);
        if (ks is null) return false;
        db.Ksiazki.Remove(ks);
        await db.SaveChangesAsync();
        return true;
    }
}
