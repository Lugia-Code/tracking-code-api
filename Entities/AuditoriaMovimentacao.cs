using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tracking_code_api;

[Table("AUDITORIA_MOVIMENTACAO")]
public class AuditoriaMovimentacao
{
    [Key]
    [Column("id_audit")]
    public int IdAudit { get; set; }

    [Required]
    [Column("id_funcionario")]
    public int IdFuncionario { get; set; }

    [Required]
    [Column("tipo_operacao")]
    public string TipoOperacao { get; set; }

    [Required]
    [Column("data_operacao")]
    public DateTime DataOperacao { get; set; }

    [Column("valores_novos")]
    public string? ValoresNovos { get; set; }

    [Column("valores_anteriores")]
    public string? ValoresAnteriores { get; set; }

    // Propriedade de navegação
    [ForeignKey("IdFuncionario")]
    public virtual Usuario Usuario { get; set; }
}