using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VGR.Domain;
using VGR.Domain.SharedKernel;

namespace VGR.Infrastructure.EF.Configs;

internal sealed class VårdvalConfig : IEntityTypeConfiguration<Vårdval>
{
    public void Configure(EntityTypeBuilder<Vårdval> b)
    {
        b.ToTable("Vårdval");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
         .HasConversion(v => v.Value, v => new VårdvalId(v))
         .ValueGeneratedNever();

        b.Property(x => x.PersonId)
         .HasConversion(v => v.Value, v => new PersonId(v));

        b.Property(x => x.EnhetsHsaId)
         .HasConversion(v => v.Value, v => HsaId.Tolka(v))
         .HasMaxLength(64).IsUnicode(false).IsRequired();

        b.ComplexProperty(x => x.Period, nb =>
        {
            nb.Property(p => p.Start).HasColumnName("Start").IsRequired();
            nb.Property(p => p.Slut).HasColumnName("Slut");
        });

        b.Property(x => x.SkapadTid).IsRequired();
        b.Property(x => x.RowVersion)
         .IsConcurrencyToken()
         .ValueGeneratedOnAddOrUpdate()
         .HasDefaultValue(new byte[] { 0 });

        // EF Core 8: cannot include complex members in HasIndex
        // Keep a simpler composite index if useful for typical lookups:
        b.HasIndex(v => new { v.PersonId, v.EnhetsHsaId });
    }
}