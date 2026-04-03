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
        // ADR-010 §8: Person.RegionId är en explicit domänegenskap — referera typat
        b.HasMany(r => r.AllaPersoner)
         .WithOne()
         .HasForeignKey(p => p.RegionId)
         .OnDelete(DeleteBehavior.Restrict);

        b.Property(x => x.RowVersion)
         .IsConcurrencyToken()
         .ValueGeneratedOnAddOrUpdate()
         .HasDefaultValue(new byte[] { 0 });

        var nav = b.Metadata.FindNavigation(nameof(Region.AllaPersoner))!;
        nav.SetField(nameof(Region.Personer)); // explicit backing field
        nav.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}