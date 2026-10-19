namespace PMS.Application.DTOs.Requests;

public record MSCallbackRequest(
    string Code,
    string RedirectUri
);
