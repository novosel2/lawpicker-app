using System.ComponentModel.DataAnnotations;
using Domain.Entities;

namespace Application.Dto.Auth;

public class RegisterAuthRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Compare("ConfirmPassword", ErrorMessage = "The password and confirmation password do not match.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;

    public AppUser ToAppUser()
    {
        return new AppUser
        {
            UserName = Username,
            Email = Email
        };
    }
}
