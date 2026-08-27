using System.Security.Claims;

namespace ResumeAnalysis.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetCandidateId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? user.FindFirstValue("sub")
                 ?? throw new UnauthorizedAccessException("Missing candidate identity.");
        if (!Guid.TryParse(id, out var guid))
            throw new UnauthorizedAccessException("Invalid candidate identity.");
        return guid;
    }

    public static string GetEmail(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email") ?? string.Empty;
}
