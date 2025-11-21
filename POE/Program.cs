using POE.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

builder.Services.AddSingleton<DatabaseService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// Enable session BEFORE routing endpoints
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ? ADD THIS TEMPORARY TEST ROUTE
app.MapGet("/test-claims", () => "Claims route is working!");
