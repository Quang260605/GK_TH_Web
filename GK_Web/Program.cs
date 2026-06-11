using GK_Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Make passwords simpler for development/testing
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    
    // User configuration
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Cookie Settings for Redirects
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Configure Google Authentication conditionally (only if valid keys are provided)
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

var authBuilder = builder.Services.AddAuthentication();
if (!string.IsNullOrEmpty(googleClientId) && !googleClientId.StartsWith("YOUR-"))
{
    authBuilder.AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = googleClientId;
        googleOptions.ClientSecret = googleClientSecret!;
    });
}

var app = builder.Build();

// Automatically Seed Database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra khi seed dữ liệu vào database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve static files
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Custom route for /courses to redirect to course catalog (Home/Index)
app.MapControllerRoute(
    name: "courses",
    pattern: "courses",
    defaults: new { controller = "Home", action = "Index" });

// Custom route for /home (Question 5 requirement)
app.MapControllerRoute(
    name: "home",
    pattern: "home",
    defaults: new { controller = "Home", action = "Index" });

// Default routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
