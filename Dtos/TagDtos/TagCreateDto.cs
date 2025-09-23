using System.ComponentModel.DataAnnotations;

namespace tracking_code_api.Dtos.TagDtos;

public record TagCreateDto
{
    [Required(ErrorMessage = "O Código da Tag é obrigatório.")]
    [StringLength(8, ErrorMessage = "O código da tag deve ter no máximo 8 caracteres.")]
    public string CodigoTag { get; set; } = string.Empty;

    // O status e a data de vínculo podem ser gerados pelo servidor
    // na criação. Não precisam vir no DTO.
}

//public record MotoCreateDto
// {
//     [StringLength(10, ErrorMessage = "A Placa deve ter no máximo 10 caracteres.")]
//     public string? Placa { get; set; } // placa opcional
// 
//     [Required(ErrorMessage = "O Chassi da moto é obrigatório.")]
//     [StringLength(20, ErrorMessage = "O Chassi deve ter no máximo 20 caracteres.")]
//     public string Chassi { get; set; }
// 
//     [Required]
//     public string Modelo { get; set; }
// 
//     public int IdSetor { get; set; }
// 
//     public string CodigoTag { get; set; }
// }