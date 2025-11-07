using VGR.Domain.SharedKernel;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Domain;

public sealed class Region
{
    public RegionId Id { get; private init; }
    public string Kod { get; private set; } = string.Empty;

    internal readonly List<Person> Personer = [];
    public IReadOnlyList<Person> AllaPersoner => Personer;

    private readonly List<IDomainEvent> _events = [];
    private void Raise(IDomainEvent e) => _events.Add(e);
    public IReadOnlyList<IDomainEvent> DequeueEvents() { var c = _events.ToArray(); _events.Clear(); return c; }

    private Region() { }
    public static Region Skapa(string kod) => new() { Id = new(), Kod = kod };

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
public sealed record PersonSkapad(RegionId RegionId, PersonId PersonId, Personnummer Personnummer, DateTimeOffset OccurredAt) 
    : DomainEvent(OccurredAt);
