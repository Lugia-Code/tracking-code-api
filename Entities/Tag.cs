using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tracking_code_api;

[Table("TAG")]
public class Tag
{
    [Key]
    [Column("codigo_tag")]
    public string CodigoTag { get; set; }

    [Required] 
    [Column("status")] public string Status { get; set; } = "inativo";

    [Required]
    [Column("data_vinculo")]
    public DateTime DataVinculo { get; set; } = DateTime.Now;
    
    // Chave estrangeira para Moto
    [Column("chassi")]
    public string? Chassi { get; set; }
    
    // Propriedades de navegação
    public virtual Moto? Moto { get; set; }

    // Relacionamento um-para-muitos com Localizacao
    public virtual ICollection<Localizacao> Localizacoes { get; set; } = new List<Localizacao>();
    
    // Regra de negócio: ao vincular moto -> ativa a tag
    public void VincularMoto(string chassi)
    {
        Chassi = chassi;
        Status = "ativo";
        DataVinculo = DateTime.Now;
    }
}