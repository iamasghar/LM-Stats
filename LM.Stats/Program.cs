using LM.Stats.Data;
using LM.Stats.Services;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using Serilog;
using SQLitePCL;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

Log.Information("Starting web application");

builder.Host.UseSerilog(); // <-- Add this line

// Rest of your existing configuration...
builder.Services.AddSingleton(Log.Logger);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure database based on settings
var dbProvider = builder.Configuration["GoogleSettings:DatabaseProvider"];
var connectionString = dbProvider switch
{
    "SQLite" => builder.Configuration.GetConnectionString("SQLiteConnection"),
    "SqlServer" => builder.Configuration.GetConnectionString("DefaultConnection"),
    _ => throw new Exception($"Unsupported database provider: {dbProvider}")
};

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider == "SQLite")
    {
        Batteries.Init();
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});


// Register services
builder.Services.AddScoped<GoogleSheetsService>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<GoogleDriveService>();
builder.Services.AddScoped<StatsProcessorService>();

var app = builder.Build();

// Ensure database is created and Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if(dbProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
    {
        dbContext.Database.EnsureCreated();
    }
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();