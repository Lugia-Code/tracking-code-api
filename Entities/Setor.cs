using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace tracking_code_api.Entities;

[Table("SETOR")]
public class Setor
{
    [Key]
    [Column("id_setor")]
    public int IdSetor { get; set; }
    
    [Required]
    [Column("nome")]
    public string Nome { get; set; }
    
    [Column("descricao")]
    public string? Descricao { get; set; }
    
    [Column("coordenadas_limite")]
    public double? CoordenadasLimite { get; set; }
    
    [JsonIgnore]
    public ICollection<Moto> Motos { get; set; } = new List<Moto>(); // Adicionamos uma coleção para o relacionamento
    public ICollection<Localizacao> Localizacoes { get; set; } = new List<Localizacao>();
}