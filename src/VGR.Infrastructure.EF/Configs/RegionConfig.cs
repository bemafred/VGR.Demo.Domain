using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VGR.Domain;
using VGR.Domain.SharedKernel;

namespace VGR.Infrastructure.EF.Configs;

internal sealed class RegionConfig : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> b)
    {
        b.ToTable(nameof(Region));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id)
         .HasConversion(v => v.Value, v => new(v))
         .ValueGeneratedNever();

        // Map collection navigation via property and set backing field
        b.HasMany(r => r.AllaPersoner)
         .WithOne()
         .HasForeignKey(nameof(RegionId))
         .OnDelete(DeleteBehavior.Restrict);

        var nav = b.Metadata.FindNavigation(nameof(Region.AllaPersoner))!;
        nav.SetField(nameof(Region.Personer)); // explicit backing field
        nav.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}