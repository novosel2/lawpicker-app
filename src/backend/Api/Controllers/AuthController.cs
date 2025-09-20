using Application.Dto.Auth;
using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("/api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }


    // POST: /api/auth/register

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterAuthRequest registerRequest)
    {
        AuthResponse response = await _authService.RegisterAsync(registerRequest);

        return Ok(response);
    }


    // POST: /api/auth/login

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginAuthRequest loginRequest)
    {
        AuthResponse response = await _authService.LoginAsync(loginRequest);
        return Ok(response);
    }


    // DELETE: /api/auth

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        await _authService.DeleteAsync();
        return Ok("Account deleted successfully.");
    }


    // PUT: /api/auth/username

    [Authorize]
    [HttpPut("username")]
    public async Task<IActionResult> UpdateUsername(string username)
    {
        await _authService.UpdateUsernameAsync(username);
        return Ok(new { 
            username = username, 
            message = "Username updated successfully"
        });
    }


    // PUT: /api/auth/email
    
    [Authorize]
    [HttpPut("email")]
    public async Task<IActionResult> UpdateEmail(string email)
    {
        await _authService.UpdateEmailAsync(email);
        return Ok(new {
            email = email,
            message = "Email updated successfully"
        });
    }
}
