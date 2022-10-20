namespace LazyAndrew.Exceptions;

public class HashNotVerified : Exception
{
    public HashNotVerified()
    {
    }

    public HashNotVerified(string? message) : base(message)
    {
    }

    public HashNotVerified(string? message, Exception? inner) : base(message, inner)
    {
    }
}