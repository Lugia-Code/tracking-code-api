using System.ComponentModel.DataAnnotations;

namespace tracking_code_api.Dtos.AuditoriaDtos;

public record AuditoriaCreateDto {
    public string PlacaMoto { get; set; }
    
    [Required(ErrorMessage = "A descrição da Auditoria é obrigatória.")]
    public string Descricao { get; set; }
    
    [Required(ErrorMessage = "A data de início da Auditoria é obrigatória.")]
    public DateTime DataInicio { get; set; }
    public DateTime DataConclusao { get; set; }
    public decimal Custo { get; set; }
    
    
    [Required(ErrorMessage = "A identificação do usuário numa auditoria é obrigatória.")]
    public int UsuarioId { get; set; }

}