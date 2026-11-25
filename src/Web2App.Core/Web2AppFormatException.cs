namespace Web2App.Core;

/// <summary>A <c>.web2app</c> file that cannot be read or does not describe a valid app.</summary>
public sealed class Web2AppFormatException : Exception
{
    public Web2AppFormatException(string message)
        : base(message)
    {
    }

    public Web2AppFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
