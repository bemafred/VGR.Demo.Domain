using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VGR.Domain;
using VGR.Domain.SharedKernel;

namespace VGR.Infrastructure.EF.Configs;

internal sealed class PersonConfig : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> b)
    {
        b.ToTable(nameof(Person));
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
         .HasConversion(v => v.Value, v => new(v))
         .ValueGeneratedNever();

        b.Property(x => x.Personnummer)
         .HasConversion(v => v.Value, v => Personnummer.Tolka(v))
         .HasMaxLength(12).IsUnicode(false).IsRequired();

        b.Property(x => x.SkapadTid).IsRequired();
        b.Property(x => x.RowVersion)
         .IsConcurrencyToken()
         .ValueGeneratedOnAddOrUpdate()
         .HasDefaultValue(new byte[] { 0 });

        // Collection navigation: map the property and use the backing field
        b.HasMany(p => p.AllaVårdval)
         .WithOne()
         .HasForeignKey(v => v.PersonId)
         .OnDelete(DeleteBehavior.Restrict);

        var nav = b.Metadata.FindNavigation(nameof(Person.AllaVårdval))!;
        nav.SetField(nameof(Person.Vårdval));
        nav.SetPropertyAccessMode(PropertyAccessMode.Field);

        // Ensure the shadow FK RegionId exists with a known type before indexing it
        b.Property<RegionId>(nameof(RegionId))
         .HasConversion(v => v.Value, v => new RegionId(v));

        b.HasIndex(nameof(RegionId), nameof(Personnummer)).IsUnique(false);
    }
}