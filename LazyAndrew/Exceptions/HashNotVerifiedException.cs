namespace LazyAndrew.Exceptions;

public class HashNotVerifiedException : Exception
{
    public HashNotVerifiedException()
    {
    }

    public HashNotVerifiedException(string? message) : base(message)
    {
    }

    public HashNotVerifiedException(string? message, Exception? inner) : base(message, inner)
    {
    }
}