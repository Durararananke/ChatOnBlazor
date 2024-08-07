using Microsoft.AspNetCore.Identity;

namespace BlazorChat.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string AvatarUrl { get; set; }
    }

}
