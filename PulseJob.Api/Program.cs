using Microsoft.EntityFrameworkCore;
using PulseJob.Api.Data;
using PulseJob.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// SQLite Database configuration
var connectionString = builder.Configuration.GetConnectionString("PulseJobDb") ?? "Data Source=pulsejob.db";
builder.Services.AddDbContext<PulseJobDbContext>(options =>
    options.UseSqlite(connectionString));

// Register typed HTTP clients & modular domain services
builder.Services.AddHttpClient<IApifyService, ApifyService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddHttpClient<IAiMatchingService, AiMatchingService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});

// Configure CORS for Angular Dashboard (Default dev port 4200)
builder.Services.AddCors(options =>
{
    options.AddPolicy("PulseJobCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Auto-migrate single table database schema on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PulseJobDbContext>();
    db.Database.EnsureCreated();
}

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("PulseJobCorsPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();
