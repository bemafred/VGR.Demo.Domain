namespace VGR.Technical;

public readonly struct Outcome
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private Outcome(bool success, string? error)
    {
        IsSuccess = success; Error = error;
    }

    public static Outcome Ok() => new(true, null);
    public static Outcome Fail(string error) => new(false, error);
}

public readonly struct Outcome<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Outcome(bool success, T? value, string? error)
    {
        IsSuccess = success;
        Value = value; 
        Error = error;
    }
    public static Outcome<T> Ok(T value) => new(true, value, null);
    public static Outcome<T> Fail(string error) => new(false, default, error);
}