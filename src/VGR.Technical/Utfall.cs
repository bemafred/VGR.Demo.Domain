namespace VGR.Technical;

/// <summary>Resultattyp för operationer utan returvärde. Representerar lyckat utfall eller förväntat affärsfel.</summary>
public readonly struct Utfall
{
    /// <summary>Sant om operationen lyckades.</summary>
    public bool IsSuccess { get; }
    /// <summary>Felmeddelande vid misslyckande, annars <c>null</c>.</summary>
    public string? Error { get; }
    /// <summary>Valfri maskinläsbar felkod vid misslyckande, annars <c>null</c>.</summary>
    public string? Code { get; }

    private Utfall(bool success, string? error, string? code)
    {
        IsSuccess = success; Error = error; Code = code;
    }

    /// <summary>Skapar ett lyckat utfall.</summary>
    public static Utfall Ok() => new(true, null, null);
    /// <summary>Skapar ett misslyckat utfall med felmeddelande och valfri kod.</summary>
    public static Utfall Fail(string error, string? code = null) => new(false, error, code);
}

/// <summary>Resultattyp för operationer med returvärde. Representerar lyckat utfall med värde eller förväntat affärsfel.</summary>
public readonly struct Utfall<T>
{
    /// <summary>Sant om operationen lyckades.</summary>
    public bool IsSuccess { get; }
    /// <summary>Resultatvärde vid lyckat utfall, annars <c>default</c>.</summary>
    public T? Value { get; }
    /// <summary>Felmeddelande vid misslyckande, annars <c>null</c>.</summary>
    public string? Error { get; }
    /// <summary>Valfri maskinläsbar felkod vid misslyckande, annars <c>null</c>.</summary>
    public string? Code { get; }

    private Utfall(bool success, T? value, string? error, string? code)
    {
        IsSuccess = success;
        Value = value;
        Error = error;
        Code = code;
    }

    /// <summary>Skapar ett lyckat utfall med angivet värde.</summary>
    public static Utfall<T> Ok(T value) => new(true, value, null, null);
    /// <summary>Skapar ett misslyckat utfall med felmeddelande och valfri kod.</summary>
    public static Utfall<T> Fail(string error, string? code = null) => new(false, default, error, code);
}
