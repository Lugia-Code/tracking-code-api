using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace tracking_code_api;

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

//     // Mapeamento para a tabela SETOR
//     [Table("SETOR")]
//     public class Setor
//     {
//         [Key]
//         [Column("id_setor")]
//         public int IdSetor { get; set; }
// 
//         [Column("nome")]
//         public string Nome { get; set; }
// 
//         [Column("descricao")]
//         public string Descricao { get; set; }
// 
//         [Column("coordenadas_limite")]
//         public string CoordenadasLimite { get; set; }
// 
//         // Relacionamento um-para-muitos com Moto
//         public virtual ICollection<Moto> Motos { get; set; }
//         
//         // Relacionamento um-para-muitos com Localizacao
//         public virtual ICollection<Localizacao> Localizacoes { get; set; }
//     }