namespace PMS.Application.DTOs.Responses;

// Shaped to match what pmsfrontend's UserAutocomplete.normalizeUser() reads directly
// (id, display_name, mail) — no transformation needed on the frontend side.
public record AzureUserSearchResult(
    string Id,
    string? DisplayName,
    string? Mail,
    string? UserPrincipalName,
    string? GivenName,
    string? Surname
);
