using System.Security.Claims;

namespace AlumniAssociationSystem.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public static string GetUserName(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name) ?? "未知用户";
}
