using Application.Dto.Auth;

namespace Application.Interfaces.IServices;

public interface IAuthService
{
    /// <summary>
    /// Registers a new user with the provided registration request.
    /// </summary>
    /// <param name="registerRequest">Register request with user information</param>
    /// <returns>Auth response with user information and JWT</returns>
    public Task<AuthResponse> RegisterAsync(RegisterAuthRequest registerRequest);

    /// <summary>
    /// Logs in a user with the provided login request.
    /// </summary>
    /// <param name="loginRequest">Login request with user information</param>
    /// <returns>Auth response with user information and JWT</returns>
    public Task<AuthResponse> LoginAsync(LoginAuthRequest loginRequest);

    /// <summary>
    /// Deletes the currently authenticated user's account.
    /// </summary>
    public Task DeleteAsync();

    /// <summary>
    /// Updates a current users username
    /// </summary>
    /// <param name="username">Updated username</param>
    public Task UpdateUsernameAsync(string username);

    /// <summary>
    /// Updates a current users username
    /// </summary>
    /// <param name="username">Updated username</param>
    public Task UpdateEmailAsync(string email);
}
