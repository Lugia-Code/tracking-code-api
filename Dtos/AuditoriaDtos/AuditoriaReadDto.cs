namespace tracking_code_api.Dtos.AuditoriaDtos;

public record AuditoriaReadDto
(
    int IdFuncionario,
    Usuario Usuario,
    string TipoOperacao,
    DateTime DataOperacao,
    string ValoresNovos, 
    string ValoresAnteriores
);