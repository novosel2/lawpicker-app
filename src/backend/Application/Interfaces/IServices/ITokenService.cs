using Domain.Entities;

namespace Application.Interfaces.IServices;

public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token for the user with the specified role.
    /// </summary>
    /// <param name="user">User object</param>
    /// <param name="role">Role name</param>
    /// <returns>Generated JWT token</returns>
    public string GenerateToken(AppUser user, string role);
}
