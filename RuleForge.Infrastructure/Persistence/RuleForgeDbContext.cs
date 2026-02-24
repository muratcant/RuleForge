using Microsoft.EntityFrameworkCore;
using RuleForge.Domain.Rules;

namespace RuleForge.Infrastructure.Persistence;

public class RuleForgeDbContext : DbContext
{
    public RuleForgeDbContext(DbContextOptions<RuleForgeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Rule> Rules => Set<Rule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RuleForgeDbContext).Assembly);
    }
}
