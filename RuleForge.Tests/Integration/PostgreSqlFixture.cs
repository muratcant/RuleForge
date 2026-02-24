using Microsoft.EntityFrameworkCore;
using RuleForge.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RuleForge.Tests.Integration;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();

        var options = new DbContextOptionsBuilder<RuleForgeDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var dbContext = new RuleForgeDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<PostgreSqlFixture>;
