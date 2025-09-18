namespace Application.Interfaces.IServices;
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the ID of the currently authenticated user.
    /// </summary>
    /// <returns>The ID of the current user, or null if not authenticated.</returns>
    public Guid? GetCurrentUserId();
}
