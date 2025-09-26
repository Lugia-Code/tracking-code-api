using System.ComponentModel.DataAnnotations;

namespace tracking_code_api.Dtos.TagDtos;

public record VincularTagDto(
    [Required(ErrorMessage = "Código da tag é obrigatório")]
    string CodigoTag
);