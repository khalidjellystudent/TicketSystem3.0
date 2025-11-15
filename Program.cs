using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container FIRST
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
//builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(conn));    **** this is the standard for local host




// for hosting on rail
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(conn));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
    });

// lan hosting 
//builder.WebHost.ConfigureKestrel(opts =>
//{
//    opts.ListenAnyIP(5000); // listens on http://*:5000
//});

// Only call this ONCE
builder.Services.AddControllersWithViews();

builder.Services.Configure<PlateRecognizerOptions>(
    builder.Configuration.GetSection("PlateRecognizer"));

builder.Services.AddHttpClient<IPlateRecognizerClient, PlateRecognizerClient>();





var app = builder.Build();






///   //// azura code
///
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}


// Middleware ORDER IS CRITICAL
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // This enables wwwroot files (CSS/JS)
app.UseRouting();

// Authentication BEFORE Authorization
app.UseAuthentication(); 
app.UseAuthorization();

// Map routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();