using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RuleForge.Domain.Rules;

namespace RuleForge.Infrastructure.Persistence.Configurations;

public class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.ToTable("rules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.IsActive)
            .IsRequired();

        builder.Property(r => r.Priority)
            .IsRequired();

        builder.Property(r => r.Conditions)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc);

        builder.HasIndex(r => r.IsActive);
        builder.HasIndex(r => r.Priority);
        builder.HasIndex(r => r.CreatedAtUtc);
    }
}
