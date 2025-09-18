using Microsoft.AspNetCore.Identity;

#pragma warning disable
namespace Domain.Entities
{
    public class AppUser : IdentityUser<Guid>
    {
        public override string Email { get; set; } = string.Empty;
        public override string UserName { get; set; } = string.Empty;

        public AppUser()
        {
            Id = Guid.NewGuid();
        }
    }
}
