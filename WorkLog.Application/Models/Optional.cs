namespace WorkLog.Application.Models;

public readonly record struct Optional<T>(
    bool IsSpecified,
    T? Value)
{
    public static Optional<T> Unspecified => new(false, default);

    public static Optional<T> Specified(T? value) => new(true, value);
}
