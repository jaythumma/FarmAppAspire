using Microsoft.AspNetCore.Identity;

namespace FarmAppAspire.Web.Data;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
