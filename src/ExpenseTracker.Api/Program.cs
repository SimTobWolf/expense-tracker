using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    string baseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=expenses.db";
    var csb = new SqliteConnectionStringBuilder(baseConnectionString) { Pooling = false };
    options.UseSqlite(csb.ToString());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program { }
