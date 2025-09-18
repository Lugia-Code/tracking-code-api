namespace tracking_code_api.Dtos.LocalizacaoDtos;

// DTO para a requisição de criação (POST)
// Usado para registrar a localização de uma tag em um setor.
public record LocalizacaoCreateDto(decimal X, decimal Y, string CodigoTag, int IdSetor);
