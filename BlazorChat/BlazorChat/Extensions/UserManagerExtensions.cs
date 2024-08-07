namespace BlazorChat.Extensions;
using BlazorChat.Data;
using Microsoft.AspNetCore.Identity;

public static class UserManagerExtensions
{
    public static Task<string> GetAvatarUrlAsync(this UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        return Task.FromResult(user.AvatarUrl);
    }

    public static Task<IdentityResult> SetAvatarUrlAsync(this UserManager<ApplicationUser> userManager, ApplicationUser user, string avatarUrl)
    {
        user.AvatarUrl = avatarUrl;
        return userManager.UpdateAsync(user);
    }
}