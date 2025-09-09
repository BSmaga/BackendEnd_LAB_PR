using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Biblioteka.Domain;
using Biblioteka.Infrastructure;
using Biblioteka.Api.DTOs;      
using Biblioteka.Api.Middleware;  

var builder = WebApplication.CreateBuilder(args);


if (builder.Environment.IsEnvironment("InMemory"))
{
    builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("BibliotekaDb"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseSqlite(builder.Configuration.GetConnectionString("db")));
}

// ========== Swagger ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Biblioteka.Api", Version = "v1" });
    c.EnableAnnotations();

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Wpisz: Bearer {twój token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


var jwtSection = builder.Configuration.GetSection("Jwt");

var key = jwtSection.GetValue<string>("Key")
          ?? throw new InvalidOperationException("Brak ustawienia Jwt:Key w appsettings.json");
var issuer = jwtSection.GetValue<string>("Issuer")
             ?? throw new InvalidOperationException("Brak ustawienia Jwt:Issuer w appsettings.json");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidIssuer = issuer,
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();


var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var problem = new
        {
            type = "https://httpstatuses.com/500",
            title = "Błąd serwera",
            status = 500,
            detail = "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później."
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});


app.UseRequestId();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRequestLogging();
app.UseAuthentication();

app.UseAuthorization();


static string Sha256Local(string s)
{
    using var sha = System.Security.Cryptography.SHA256.Create();
    return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
}

static BookDto ToBookDto(Ksiazka k) => new()
{
    Id = k.Id,
    Tytul = k.Tytul,
    Autor = k.Autor,
    Rok = k.Rok,
    ISBN = k.ISBN,
    LiczbaEgzemplarzy = k.LiczbaEgzemplarzy
};

static CzytelnikDto ToReaderDto(Czytelnik c) => new()
{
    Id = c.Id,
    Imie = c.Imie,
    Email = c.Email,
    Rola = c.Rola
};

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (db.Database.IsRelational())
        db.Database.Migrate();

    if (!db.Czytelnicy.Any())
    {
        db.Czytelnicy.AddRange(
            new Czytelnik { Imie = "Admin", Email = "admin@lib.pl", HaszHasla = Sha256Local("Pass!23"), Rola = "Admin" },
            new Czytelnik { Imie = "User", Email = "user@lib.pl", HaszHasla = Sha256Local("Pass!23"), Rola = "User" }
        );
    }

    if (!db.Ksiazki.Any())
    {
        db.Ksiazki.Add(new Ksiazka
        {
            Tytul = "Clean Architecture",
            Autor = "Robert C. Martin",
            Rok = 2017,
            ISBN = "9780134494166",
            LiczbaEgzemplarzy = 3
        });
    }

    db.SaveChanges();
}

// ========== Health ==========
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// ========== Auth: logowanie (query) -> JWT ==========
app.MapPost("/api/auth/login",
    async (AppDbContext db,
           [FromQuery, SwaggerParameter("Email użytkownika")] string email,
           [FromQuery, SwaggerParameter("Hasło użytkownika")] string haslo) =>
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(haslo))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email/haslo"] = new[] { "Email i hasło są wymagane." }
            });

        var emailNorm = email.Trim().ToLower();
        var hash = Sha256Local(haslo);

        var user = await db.Czytelnicy.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm && u.HaszHasla == hash);

        if (user is null) return Results.Unauthorized();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(ClaimTypes.Role, user.Rola),
            new Claim("uid", user.Id.ToString())
        };

        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        var jwtStr = new JwtSecurityTokenHandler().WriteToken(token);
        return Results.Ok(new { token = jwtStr });
    });

// ========== KSIĄŻKI (CRUD + filtr/sort/paginacja) ==========

// GET (lista)
app.MapGet("/api/ksiazki",
    async (AppDbContext db,
           [FromQuery, SwaggerParameter("Fraza wyszukiwania (tytuł, autor, ISBN).")] string? q,
           [FromQuery, SwaggerParameter("Numer strony (>= 1).")] int page = 1,
           [FromQuery, SwaggerParameter("Rozmiar strony (1–100).")] int pageSize = 10,
           [FromQuery, SwaggerParameter("Pole sortowania: Tytul, Autor, Rok, ISBN.")] SortField sort = SortField.Tytul,
           [FromQuery, SwaggerParameter("Sortowanie malejące?")] bool desc = false) =>
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Ksiazki.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var f = q.Trim().ToLower();
            query = query.Where(k =>
                k.Tytul.ToLower().Contains(f) ||
                k.Autor.ToLower().Contains(f) ||
                k.ISBN.ToLower().Contains(f));
        }

        query = sort switch
        {
            SortField.Autor => desc ? query.OrderByDescending(k => k.Autor) : query.OrderBy(k => k.Autor),
            SortField.Rok => desc ? query.OrderByDescending(k => k.Rok) : query.OrderBy(k => k.Rok),
            SortField.ISBN => desc ? query.OrderByDescending(k => k.ISBN) : query.OrderBy(k => k.ISBN),
            _ => desc ? query.OrderByDescending(k => k.Tytul) : query.OrderBy(k => k.Tytul),
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(k => new BookDto
            {
                Id = k.Id,
                Tytul = k.Tytul,
                Autor = k.Autor,
                Rok = k.Rok,
                ISBN = k.ISBN,
                LiczbaEgzemplarzy = k.LiczbaEgzemplarzy
            })
            .ToListAsync();

        return Results.Ok(new PagedResult<BookDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = items
        });
    });

// GET (szczegóły)
app.MapGet("/api/ksiazki/{id:int}", async (AppDbContext db, int id) =>
{
    var ks = await db.Ksiazki.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id);
    return ks is not null ? Results.Ok(ToBookDto(ks)) : Results.NotFound();
});

// POST (Admin)
app.MapPost("/api/ksiazki", async (AppDbContext db, BookCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Tytul) ||
        string.IsNullOrWhiteSpace(dto.Autor) ||
        string.IsNullOrWhiteSpace(dto.ISBN) ||
        dto.LiczbaEgzemplarzy < 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["body"] = new[] { "Uzupełnij Tytul/Autor/ISBN i podaj nieujemną liczbę egzemplarzy." }
        });
    }

    var ks = new Ksiazka
    {
        Tytul = dto.Tytul,
        Autor = dto.Autor,
        Rok = dto.Rok,
        ISBN = dto.ISBN,
        LiczbaEgzemplarzy = dto.LiczbaEgzemplarzy
    };

    db.Ksiazki.Add(ks);
    await db.SaveChangesAsync();
    return Results.Created($"/api/ksiazki/{ks.Id}", ToBookDto(ks));
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// PUT (Admin)
app.MapPut("/api/ksiazki/{id:int}", async (AppDbContext db, int id, BookCreateDto dto) =>
{
    var ks = await db.Ksiazki.FindAsync(id);
    if (ks is null) return Results.NotFound();

    if (string.IsNullOrWhiteSpace(dto.Tytul) ||
        string.IsNullOrWhiteSpace(dto.Autor) ||
        string.IsNullOrWhiteSpace(dto.ISBN) ||
        dto.LiczbaEgzemplarzy < 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["body"] = new[] { "Uzupełnij Tytul/Autor/ISBN i podaj nieujemną liczbę egzemplarzy." }
        });
    }

    ks.Tytul = dto.Tytul;
    ks.Autor = dto.Autor;
    ks.Rok = dto.Rok;
    ks.ISBN = dto.ISBN;
    ks.LiczbaEgzemplarzy = dto.LiczbaEgzemplarzy;

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// DELETE (Admin)
app.MapDelete("/api/ksiazki/{id:int}", async (AppDbContext db, int id) =>
{
    var ks = await db.Ksiazki.FindAsync(id);
    if (ks is null) return Results.NotFound();
    db.Ksiazki.Remove(ks);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// ========== CZYTELNICY (CRUD – Admin) ==========
app.MapGet("/api/czytelnicy", async (AppDbContext db) =>
{
    var items = await db.Czytelnicy.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
    return Results.Ok(items.Select(ToReaderDto));
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapGet("/api/czytelnicy/{id:int}", async (AppDbContext db, int id) =>
{
    var c = await db.Czytelnicy.FindAsync(id);
    return c is null ? Results.NotFound() : Results.Ok(ToReaderDto(c));
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapPost("/api/czytelnicy", async (AppDbContext db, CzytelnikCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Haslo))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["body"] = new[] { "Email i hasło są wymagane." }
        });

    var c = new Czytelnik
    {
        Imie = dto.Imie,
        Email = dto.Email,
        HaszHasla = Sha256Local(dto.Haslo),
        Rola = dto.Rola
    };
    db.Czytelnicy.Add(c);
    await db.SaveChangesAsync();
    return Results.Created($"/api/czytelnicy/{c.Id}", ToReaderDto(c));
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapPut("/api/czytelnicy/{id:int}", async (AppDbContext db, int id, CzytelnikCreateDto dto) =>
{
    var c = await db.Czytelnicy.FindAsync(id);
    if (c is null) return Results.NotFound();

    c.Imie = dto.Imie;
    c.Email = dto.Email;
    c.Rola = dto.Rola;
    if (!string.IsNullOrWhiteSpace(dto.Haslo))
        c.HaszHasla = Sha256Local(dto.Haslo);

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapDelete("/api/czytelnicy/{id:int}", async (AppDbContext db, int id) =>
{
    var c = await db.Czytelnicy.FindAsync(id);
    if (c is null) return Results.NotFound();
    db.Czytelnicy.Remove(c);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// ========== WYPOŻYCZENIA (autoryzowany użytkownik) ==========
app.MapPost("/api/wypozyczenia", async (AppDbContext db, ClaimsPrincipal user, int ksiazkaId) =>
{
    var uid = user.FindFirstValue("uid");
    if (uid is null) return Results.Unauthorized();

    var ks = await db.Ksiazki.FindAsync(ksiazkaId);
    if (ks is null) return Results.NotFound("Nie ma takiej książki.");
    if (ks.LiczbaEgzemplarzy <= 0) return Results.BadRequest("Brak dostępnych egzemplarzy.");

    ks.LiczbaEgzemplarzy--;
    var w = new Wypozyczenie { CzytelnikId = int.Parse(uid), KsiazkaId = ksiazkaId };
    db.Wypozyczenia.Add(w);
    await db.SaveChangesAsync();
    return Results.Ok(w);
}).RequireAuthorization();

app.MapPost("/api/wypozyczenia/{id:int}/zwrot", async (AppDbContext db, ClaimsPrincipal user, int id) =>
{
    var uid = user.FindFirstValue("uid");
    if (uid is null) return Results.Unauthorized();

    var w = await db.Wypozyczenia.FindAsync(id);
    if (w is null) return Results.NotFound();
    if (w.CzytelnikId != int.Parse(uid)) return Results.Forbid();
    if (w.DataZwrotu is not null) return Results.BadRequest("To wypożyczenie już zwrócono.");

    w.DataZwrotu = DateTime.UtcNow;
    var ks = await db.Ksiazki.FindAsync(w.KsiazkaId);
    if (ks is not null) ks.LiczbaEgzemplarzy++;
    await db.SaveChangesAsync();
    return Results.Ok(w);
}).RequireAuthorization();

app.MapGet("/api/czytelnicy/me/wypozyczenia", async (AppDbContext db, ClaimsPrincipal user) =>
{
    var uid = user.FindFirstValue("uid");
    if (uid is null) return Results.Unauthorized();
    var me = int.Parse(uid);

    var lista = await db.Wypozyczenia
        .Where(x => x.CzytelnikId == me)
        .OrderByDescending(x => x.DataWypozyczenia)
        .ToListAsync();

    return Results.Ok(lista);
}).RequireAuthorization();

app.Run();
