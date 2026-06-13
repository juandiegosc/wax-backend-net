using Domain.ProductAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations.ProductConfigurations;
 
public class QuotationRuleConfiguration : IEntityTypeConfiguration<QuotationRule>
{
    public void Configure(EntityTypeBuilder<QuotationRule> builder)
    {
        builder.ToTable("QuotationRules");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Key).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.Key).IsUnique();

        builder.Property(p => p.Value).HasPrecision(18, 4);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.IsDefault).IsRequired().HasDefaultValue(false);

        builder.Ignore(p => p.DomainEvents);
    }
}