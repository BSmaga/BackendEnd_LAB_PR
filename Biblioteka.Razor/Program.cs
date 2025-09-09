using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;


builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/Denied";
        o.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Biblioteka.Razor.Services.ITokenStore, Biblioteka.Razor.Services.CookieTokenStore>();
builder.Services.AddTransient<Biblioteka.Razor.Services.TokenHandler>();


builder.Services.AddHttpClient("api", c =>
{
    c.BaseAddress = new Uri(cfg["Api:BaseUrl"]!);
}).AddHttpMessageHandler<Biblioteka.Razor.Services.TokenHandler>();

builder.Services.AddHttpClient("gql", c =>
{
    c.BaseAddress = new Uri(cfg["GraphQL:Endpoint"]!);
});

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.Run();
