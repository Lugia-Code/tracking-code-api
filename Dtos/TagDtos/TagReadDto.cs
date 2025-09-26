namespace tracking_code_api.Dtos.TagDtos;

public record TagReadDto(
    string CodigoTag,
    string Status,
    DateTime? DataVinculo
    );