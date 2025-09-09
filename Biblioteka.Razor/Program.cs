using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Biblioteka.Razor.Services;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// cookie auth
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
builder.Services.AddScoped<ITokenStore, CookieTokenStore>();
builder.Services.AddTransient<TokenHandler>();

builder.Services.AddHttpClient("api", c =>
{
    c.BaseAddress = new Uri(cfg["Api:BaseUrl"]!); // np. http://localhost:5180/
}).AddHttpMessageHandler<TokenHandler>();

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.Run();
