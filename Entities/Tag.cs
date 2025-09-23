using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace tracking_code_api.Entities;

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
    [JsonIgnore]
    public virtual Moto? Moto { get; set; }

    // Relacionamento um-para-muitos com Localizacao
    [JsonIgnore]
    public virtual ICollection<Localizacao> Localizacoes { get; set; } = new List<Localizacao>();
    
    // Regra de negócio: ao vincular moto -> ativa a tag
    public void VincularMoto(string chassi)
    {
        if (string.IsNullOrWhiteSpace(chassi))
            throw new ArgumentException("Chassi não pode ser nulo ou vazio", nameof(chassi));
            
        if (!string.IsNullOrEmpty(Chassi) && Chassi != chassi)
            throw new InvalidOperationException($"Tag já está vinculada à moto com chassi: {Chassi}");
        
        Chassi = chassi;
        Status = "ativo";
        DataVinculo = DateTime.Now;
    }
    
    // Método para desvincular uma moto da tag (desativa a tag)
    public void DesvincularMoto()
    {
        Chassi = null;
        Status = "inativo";
        // Mantém a DataVinculo para histórico
    }
    
    // Propriedade para verificar se a tag está disponível
    [JsonIgnore]
    public bool EstaDisponivel => string.IsNullOrEmpty(Chassi) || Status == "inativo";

    // Propriedade para verificar se a tag está ativa
    [JsonIgnore]
    public bool EstaAtiva => Status == "ativo" && !string.IsNullOrEmpty(Chassi);
}