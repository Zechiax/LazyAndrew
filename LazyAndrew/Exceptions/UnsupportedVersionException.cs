namespace LazyAndrew.Exceptions;

public class UnsupportedVersionException : Exception
{
    public UnsupportedVersionException()
    {
    }

    public UnsupportedVersionException(string? message) : base(message)
    {
    }

    public UnsupportedVersionException(string? message, Exception? inner) : base(message, inner)
    {
    }
}