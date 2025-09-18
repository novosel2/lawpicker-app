using Microsoft.AspNetCore.Identity;
using Application.Dto.Auth;
using Application.Interfaces.IServices;
using Domain.Entities;
using Application.Exceptions;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;

    public AuthService(UserManager<AppUser> userManager, ITokenService tokenService, ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
    }


    public async Task<AuthResponse> RegisterAsync(RegisterAuthRequest registerRequest)
    {
        AppUser user = registerRequest.ToAppUser();

        IdentityResult createdResult = await _userManager.CreateAsync(user, registerRequest.Password);

        if (!createdResult.Succeeded)
        {
            throw new RegisterFailedException(
                string.Join(" | ", createdResult.Errors.Select(e => e.Description)));
        }

        IdentityResult roleResult = await _userManager.AddToRoleAsync(user, "User");

        if (!roleResult.Succeeded)
        {
            _ = await _userManager.DeleteAsync(user);

            throw new RoleAssignmentFailedException(
                string.Join(" | ", roleResult.Errors.Select(e => e.Description)));
        }

        string token = _tokenService.GenerateToken(user, "User");
        AuthResponse authResponse = new()
        {
            Username = user.UserName!,
            Email = user.Email!,
            Token = token
        };

        return authResponse;
    }

    
    public async Task<AuthResponse> LoginAsync(LoginAuthRequest loginRequest)
    {
        AppUser user = _userManager.Users.FirstOrDefault(u => u.UserName == loginRequest.LoginName || u.Email == loginRequest.LoginName)
            ?? throw new InvalidCredentialsException("Invalid username or password");

        bool result = await _userManager.CheckPasswordAsync(user, loginRequest.Password);
        if (!result)
        {
            throw new InvalidCredentialsException("Invalid username or password");
        }

        var roles = await _userManager.GetRolesAsync(user);

        string token = _tokenService.GenerateToken(user, roles[0]);
        AuthResponse authResponse = new()
        {
            Username = user.UserName!,
            Email = user.Email!,
            Token = token
        };

        return authResponse;
    }


    public async Task DeleteAsync()
    {
        AppUser user = await GetCurrentUserAsync();

        IdentityResult result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new SavingChangesFailedException(
                string.Join(" | ", result.Errors.Select(e => e.Description)));
        }
    }
    

    public async Task UpdateUsernameAsync(string username)
    {
        AppUser user = await GetCurrentUserAsync();

        IdentityResult result = await _userManager.SetUserNameAsync(user, username);
        if (!result.Succeeded)
        {
            throw new SavingChangesFailedException(
                    string.Join(" | ", result.Errors.Select(e => e.Description)));
        } 
    }

    public async Task UpdateEmailAsync(string email)
    {
        AppUser user = await GetCurrentUserAsync();

        IdentityResult result = await _userManager.SetEmailAsync(user, email);
        if (!result.Succeeded)
        {
            throw new SavingChangesFailedException(
                    string.Join(" | ", result.Errors.Select(e => e.Description)));
        }
    }


    private async Task<AppUser> GetCurrentUserAsync()
    {
        Guid currentUserId = _currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("You are not authenticated.");

        AppUser user = await _userManager.FindByIdAsync(currentUserId.ToString())
            ?? throw new NotFoundException($"User not found, ID {currentUserId}");
        
        return user;
    }
}
