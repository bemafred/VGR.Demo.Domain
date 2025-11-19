


namespace VGR.Domain.Queries;

public static class VårdvalQueries // TODO: Experimentella vyer och projektioner för att eventuellt etablera nya mönster
{
    extension(Vårdval self)
    {
        public string ExperimentalProperty => $"{self.PersonId.ToString()} {self.Id.ToString()}";
    }
}

