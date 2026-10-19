using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quiz.Data.Entities;

namespace Quiz.Data.Configurations;

internal sealed class ThemeConfiguration : IEntityTypeConfiguration<Theme>
{
    public void Configure(EntityTypeBuilder<Theme> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(100);
        builder.HasIndex(t => t.Name).IsUnique();

        builder.Property(t => t.Icon).HasMaxLength(50);

        builder.HasMany(t => t.Questions)
            .WithOne(q => q.Theme)
            .HasForeignKey(q => q.ThemeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
