namespace PMS.Application.DTOs.Responses;

public record SyncUsersResponse(
    int TotalFetchedFromAzure,
    int Created,
    int Updated,
    int Skipped
);
