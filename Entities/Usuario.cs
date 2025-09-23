using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tracking_code_api.Entities;

[Table("USUARIO")]
public class Usuario
{
    [Key]
    [Column("id_funcionario")]
    public int IdFuncionario { get; set; }
    
    [Required]
    [Column("nome")]
    public string Nome { get; set; }
    
    [Required]
    [Column("email")]
    public string Email { get; set; }
    
    [Required]
    [Column("senha")]
    public string Senha { get; set; }
    
    [Required]
    [Column("funcao")]
    public required string Funcao { get; set; }
    
    // Relacionamento um-para-muitos com AuditoriaMovimentacao
    public virtual ICollection<AuditoriaMovimentacao> Auditorias { get; set; } = new List<AuditoriaMovimentacao>();
}