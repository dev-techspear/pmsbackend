using System.Collections.Generic;

namespace PMS.Application.DTOs.Responses;

public record UserListResponseDto(
    int Total,
    IEnumerable<UserResponseDto> Items
);
