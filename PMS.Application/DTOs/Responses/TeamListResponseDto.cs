using System.Collections.Generic;

namespace PMS.Application.DTOs.Responses;

public record TeamListResponseDto(
    int Total,
    IEnumerable<TeamResponseDto> Items
);
