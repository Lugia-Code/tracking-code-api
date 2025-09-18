namespace tracking_code_api.Dtos.AuditoriaDtos;

public class AuditoriaUpdateDto
{
    public string TipoOperacao { get; set; }
    public string ValoresNovos { get; set; }
    public string ValoresAnteriores { get; set; }
}

//    public record AuditoriaUpdateDto
// (string Descricao, DateTime DataConclusao, decimal Custo);
