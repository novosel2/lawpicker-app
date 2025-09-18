using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Auth;

public class LoginAuthRequest
{
    [Required]
    public string LoginName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
