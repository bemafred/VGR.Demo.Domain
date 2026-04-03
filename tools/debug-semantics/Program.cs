using Microsoft.EntityFrameworkCore;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Semantics.Linq;
using VGR.Technical.Testing;

await using var h = new SqliteHarness();
var region = Region.Skapa("14");
var person = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), DateTimeOffset.UtcNow);
person.SkapaVårdval(HsaId.Tolka("HSA-1"),
    Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
    DateTimeOffset.UtcNow);
h.Write.Regioner.Add(region);
await h.Write.SaveChangesAsync();

var now = DateTimeOffset.UtcNow;

Test("Start <= now",
    () => h.Read.Vårdval.Where(v => v.Period.Start <= now).ToQueryString());

Test("Full Innehåller inline",
    () => h.Read.Vårdval.Where(v => v.Period.Start <= now && (v.Period.Slut == null || now < v.Period.Slut)).ToQueryString());

Test("WithSemantics Innehåller",
    () => h.Read.Vårdval.WithSemantics().Where(v => v.Period.Innehåller(now)).ToQueryString());

Test("WithSemantics ÄrTillsvidare",
    () => h.Read.Vårdval.WithSemantics().Where(v => v.Period.ÄrTillsvidare).ToQueryString());

Test("WithSemantics ÄrAktivt",
    () => h.Read.Vårdval.WithSemantics().Where(v => v.ÄrAktivt).ToQueryString());

static void Test(string name, Func<string> action)
{
    try
    {
        var sql = action();
        Console.WriteLine($"OK  {name}");
        Console.WriteLine($"    {sql.ReplaceLineEndings(" ")}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL {name}");
        Console.WriteLine($"    {ex.Message[..Math.Min(150, ex.Message.Length)]}\n");
    }
}
