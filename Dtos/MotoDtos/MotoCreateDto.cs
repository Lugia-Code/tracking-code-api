
using System.ComponentModel.DataAnnotations;

namespace tracking_code_api.Dtos.MotoDtos;

//public class MotoCreateDto
// {
//     public string Placa { get; set; }
// 
//     [Required]
//     public string Chassi { get; set; }
// 
//     [Required]
//     public string Modelo { get; set; }
// 
//     [Required]
//     public int SetorId { get; set; }
// 
//     public string Tag { get; set; }
// }

// Record para a requisição de criação (POST)
public record MotoCreateDto
{
    [StringLength(10, ErrorMessage = "A Placa deve ter no máximo 10 caracteres.")]
    public string? Placa { get; set; }

    [Required(ErrorMessage = "O Chassi da moto é obrigatório.")]
    [StringLength(20, ErrorMessage = "O Chassi deve ter no máximo 20 caracteres.")]
    public string Chassi { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Modelo da moto é obrigatório.")]
    public string Modelo { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Setor é obrigatório.")]
    public int IdSetor { get; set; }

    [Required(ErrorMessage = "A Tag é obrigatória.")]
    public string CodigoTag { get; set; } = string.Empty;
}