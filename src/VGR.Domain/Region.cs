using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Abstractions;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain;

/// <summary>Aggregatrot för en administrativ region (t.ex. Västra Götalandsregionen).</summary>
public sealed class Region
{
    /// <summary>Unikt id för regionen.</summary>
    public RegionId Id { get; private init; }
    /// <summary>Regionens administrativa kod (t.ex. "14" för VGR).</summary>
    public string Kod { get; private set; } = string.Empty;
    /// <summary>Optimistisk samtidighetstoken.</summary>
    public byte[] RowVersion { get; private set; } = [];

    internal readonly List<Person> Personer = [];
    /// <summary>Alla personer registrerade i denna region.</summary>
    public IReadOnlyList<Person> AllaPersoner => Personer;

    private readonly List<IDomainEvent> _events = [];
    private void Raise(IDomainEvent e) => _events.Add(e);
    /// <summary>Hämtar och rensar köade domänhändelser.</summary>
    public IReadOnlyList<IDomainEvent> DequeueEvents() { var c = _events.ToArray(); _events.Clear(); return c; }

    private Region() { }
    /// <summary>Skapar en ny region med given kod.</summary>
    public static Region Skapa(string kod) => new() { Id = RegionId.Nytt(), Kod = kod };

    /// <summary>Skapar och registrerar en ny person i regionen. Kastar vid dubblettnamn.</summary>
    public Person SkapaPerson(Personnummer pnr, DateTimeOffset nu)
    {
        if (Personer.Any(p => p.Personnummer == pnr))
            Throw.Person.Dubblett(pnr);

        var person = Person.Skapa(Id, pnr, nu);
        Personer.Add(person);

        Raise(new PersonSkapad(Id, person.Id, pnr, nu));

        return person;
    }
}
/// <summary>Domänhändelse: en person har skapats i en region.</summary>
public sealed record PersonSkapad(RegionId RegionId, PersonId PersonId, Personnummer Personnummer, DateTimeOffset OccurredAt)
    : DomainEvent(OccurredAt);
