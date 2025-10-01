namespace Application.Exceptions;

public class RoleAssignmentFailedException : Exception
{
    public RoleAssignmentFailedException()
    {
    }

    public RoleAssignmentFailedException(string? message) : base(message)
    {
    }
}
