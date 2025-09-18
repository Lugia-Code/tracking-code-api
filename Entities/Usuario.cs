using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tracking_code_api;

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

//motoGroup.MapGet("/{placa}", async Task<Results<Ok<Moto>, NotFound>> (string placa, MotosDbContext db) =>
// {
//     var moto = await db.Motos.Include(m => m.Setor).FirstOrDefaultAsync(m => m.Placa == placa);
//     return moto is not null
//         ? TypedResults.Ok(moto)
//         : TypedResults.NotFound();
// })
// .WithSummary("Retorna uma moto pela placa.");

//motoGroup.MapGet("/", async (MotosDbContext db) => await db.Motos
//     .Include(m => m.Setor)
//     .ToListAsync())
// .WithSummary("Retorna todas as motos.");