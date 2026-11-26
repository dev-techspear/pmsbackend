namespace PMS.Application.Common;

/// <summary>
/// Decides whether a PMS-issued token (access or refresh) predates a sign-out-everywhere.
/// Tokens carry their issue time as the "iat" claim (Unix seconds).
/// </summary>
public static class SessionRevocation
{
    public static bool IsRevoked(DateTime? sessionsValidFrom, string? issuedAtClaim)
    {
        if (sessionsValidFrom is null) return false;

        // A token with no readable issue time was made before this feature existed, so it predates the revocation.
        if (!long.TryParse(issuedAtClaim, out var issuedAt)) return true;

        var cutoff = new DateTimeOffset(DateTime.SpecifyKind(sessionsValidFrom.Value, DateTimeKind.Utc)).ToUnixTimeSeconds();
        // Strict: a token minted in the same second as the revocation is treated as old.
        return issuedAt <= cutoff;
    }
}
