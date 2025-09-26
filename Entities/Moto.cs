using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace tracking_code_api.Entities;

[Table("MOTO")]
public class Moto
{
    [Key]
    [Column("chassi")]
    public string Chassi { get; set; } // O Chassi será a chave primária
    
    [Column("placa")]
    public string? Placa { get; set; } 
    
    [Required]
    [Column("modelo")]
    public string Modelo { get; set; }
    
    [Column("data_cadastro")]
    public DateTime DataCadastro { get; set; } = DateTime.Now;
    
    [Column("codigo_tag")]
    public string CodigoTag { get; set; }
    
    [Required]
    [Column("id_setor")]
    public int IdSetor { get; set; }

    [Column("id_audit")]
    public int? IdAudit { get; set; }

    // Propriedades de navegação
    [JsonIgnore]
    public virtual Tag Tag { get; set; }
    
    [JsonIgnore]
    public virtual Setor Setor { get; set; }

    [JsonIgnore]
    public virtual AuditoriaMovimentacao? Auditoria { get; set; }
}