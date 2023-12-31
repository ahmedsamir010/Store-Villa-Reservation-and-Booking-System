using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using Store.Application.Common;
using Store.Domain.Entities;
using Store.Infrastructre.Data;
using Store.Infrastructre;
using System.Security.Claims;
using Stripe;
using Store.Application.Common.Interfaces;
using Store.Infrastructre.Repositories;
using Syncfusion.Licensing;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;

builder.Services.AddControllersWithViews();

services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.ReturnUrlParameter = "returnUrl";
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"];
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.ClientId = configuration["Authentication:Facebook:AppId"];
    facebookOptions.ClientSecret = configuration["Authentication:Facebook:AppSecret"];
})
.AddMicrosoftAccount(MicrosoftAccountOptions =>
{
    MicrosoftAccountOptions.ClientId = configuration["Authentication:Microsoft:ClientId"];
    MicrosoftAccountOptions.ClientSecret = configuration["Authentication:Microsoft:ClientSecret"];
})
.AddTwitter(twitterOptions =>
{
    twitterOptions.ConsumerKey = configuration["Authentication:Twitter:ClientId"];
    twitterOptions.ConsumerSecret = configuration["Authentication:Twitter:ClientSecret"];
    twitterOptions.RetrieveUserDetails = true;
    twitterOptions.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
    twitterOptions.ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);
})
.AddGitHub(githubOptions =>
{
    githubOptions.ClientId = configuration["Authentication:GitHub:ClientId"];
    githubOptions.ClientSecret = configuration["Authentication:GitHub:ClientSecret"];
    githubOptions.Scope.Add("user:email");
    githubOptions.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
    githubOptions.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
});

services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
});

services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
services.AddScoped(typeof(IBookingRepository), typeof(BookingRepository));

services.AddMvc().AddNToastNotifyToastr(new ToastrOptions()
{
    ProgressBar = true,
    PositionClass = ToastPositions.TopRight,
    PreventDuplicates = true,
    CloseButton = true,
});

var app = builder.Build();
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<String>();
SyncfusionLicenseProvider.RegisterLicense(builder.Configuration.GetSection("Syncfusion:licensekey").Get<String>());
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
