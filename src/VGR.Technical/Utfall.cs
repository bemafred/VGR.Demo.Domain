namespace VGR.Technical;

public readonly struct Utfall
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private Utfall(bool success, string? error)
    {
        IsSuccess = success; Error = error;
    }

    public static Utfall Ok() => new(true, null);
    public static Utfall Fail(string error) => new(false, error);
}

public readonly struct Utfall<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Utfall(bool success, T? value, string? error)
    {
        IsSuccess = success;
        Value = value; 
        Error = error;
    }

    public static Utfall<T> Ok(T value) => new(true, value, null);
    public static Utfall<T> Fail(string error) => new(false, default, error);
}