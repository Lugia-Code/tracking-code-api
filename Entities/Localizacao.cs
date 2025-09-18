using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tracking_code_api;

[Table("LOCALIZACAO")]
public class Localizacao
{
    [Key]
    [Column("id_localizacao")]
    public int IdLocalizacao { get; set; }

    [Required]
    [Column("x")]
    public decimal X { get; set; }

    [Required]
    [Column("y")]
    public decimal Y { get; set; }

    [Required]
    [Column("codigo_tag")]
    public string CodigoTag { get; set; }

    [Required]
    [Column("id_setor")]
    public int IdSetor { get; set; }

    [Column("data_registro")]
    public DateTime DataRegistro { get; set; } = DateTime.Now;

    // Propriedades de navegação
    [ForeignKey("CodigoTag")]
    public virtual Tag Tag { get; set; }

    [ForeignKey("IdSetor")]
    public virtual Setor Setor { get; set; }
}