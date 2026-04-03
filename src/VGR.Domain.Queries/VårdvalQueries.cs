namespace VGR.Domain.Queries;

/// <summary>
/// Emergence-plats för domännära queries och projektioner.
/// <para>
/// Syftet med denna klass är tvåfaldigt:
/// <list type="number">
///   <item>Etablera mönster för vyformade projektioner som lever nära domänen
///         men utanför aggregaten — t.ex. sammanställningar, sökresultat, rapportvyer.</item>
///   <item>Demonstrera C# 14 <c>extension</c>-syntax som alternativ till traditionella
///         extensionmetoder för domänkoncept som inte hör hemma i aggregatet självt.</item>
/// </list>
/// </para>
/// </summary>
public static class VårdvalQueries
{
    extension(Vårdval self)
    {
        /// <summary>Experimentell vy-property: sammanfogat person- och vårdvals-id.</summary>
        public string ExperimentalProperty => $"{self.PersonId.ToString()} {self.Id.ToString()}";
    }
}

