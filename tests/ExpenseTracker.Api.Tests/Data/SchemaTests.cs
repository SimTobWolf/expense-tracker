using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Features.Expenses;

namespace ExpenseTracker.Api.Tests.Data;

/// <summary>
/// Verifies that the EF Core migration applies cleanly to a real SQLite file
/// and that the resulting schema preserves the constraints required by the
/// domain (e.g. decimal precision on Amount).
///
/// Each test class gets its own isolated SQLite temp file via
/// CustomWebApplicationFactory. The file is deleted in Dispose so subsequent
/// runs start fresh.
/// </summary>
public class SchemaTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public SchemaTests()
    {
        _factory = new CustomWebApplicationFactory();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    // -------------------------------------------------------------------------
    // PR-A-1
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Given_a_fresh_SQLite_file_When_the_app_starts_Then_GET_health_returns_200()
    {
        // The factory boots the application. MigrateAsync() is called during
        // startup (in Program.cs). If the migration throws — e.g. because
        // the migration file does not exist or its SQL is invalid — the app
        // will fail to start and CreateClient() / GetAsync() will surface that
        // error, causing this test to fail.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // PR-A-2
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Given_the_app_has_started_When_applied_migrations_are_queried_Then_exactly_one_migration_named_InitialSchema_is_recorded()
    {
        // Boot the app so that the startup migration runs.
        _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        IEnumerable<string> applied = await context.Database.GetAppliedMigrationsAsync();

        // Exactly one migration, and its name ends with "InitialSchema".
        string[] appliedArray = applied.ToArray();
        Assert.Single(appliedArray);
        Assert.EndsWith("InitialSchema", appliedArray[0]);
    }

    // -------------------------------------------------------------------------
    // PR-A-3
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Given_an_expense_with_Amount_10_999m_When_written_and_read_back_Then_Amount_equals_10_999m_exactly()
    {
        // This is the canonical gate for HasColumnType("NUMERIC").
        // SQLite maps C# decimal to REAL (IEEE-754 double) by default.
        // A REAL cannot represent 10.999 without rounding loss.
        // If the column type is not overridden the Assert below will fail
        // because the round-tripped value will differ from 10.999m.

        // Boot the app so startup migration runs first.
        _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var expense = new Expense
        {
            Amount = 10.999m,
            Category = "Test",
            Date = new DateOnly(2026, 1, 15),
        };

        context.Expenses.Add(expense);
        await context.SaveChangesAsync();

        // Clear the EF change tracker so the next read hits the database.
        context.ChangeTracker.Clear();

        Expense? reloaded = await context.Expenses.FindAsync(expense.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(10.999m, reloaded!.Amount);
    }
}

// ---------------------------------------------------------------------------
// Test infrastructure — per-class isolated SQLite temp file
// ---------------------------------------------------------------------------

/// <summary>
/// Overrides the SQLite connection string with a per-run temp file so that
/// each test class starts against an empty, isolated database. The temp file
/// is removed in Dispose.
/// </summary>
internal sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly string _dbPath = Path.GetTempFileName();

    protected override void ConfigureWebHost(
        Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Inject an in-process override for the connection string so the
            // factory always uses the dedicated temp file rather than whatever
            // is in appsettings.json.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    $"Data Source={_dbPath}",
            });
        });
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
