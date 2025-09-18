namespace Application.Exceptions;

public class RegisterFailedException : Exception
{
    public RegisterFailedException()
    {
    }

    public RegisterFailedException(string? message) : base(message)
    {
    }
}
