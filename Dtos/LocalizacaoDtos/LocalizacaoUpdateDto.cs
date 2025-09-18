namespace tracking_code_api.Dtos.LocalizacaoDtos;

// DTO para a requisição de atualização (PUT)
// Usado para atualizar as coordenadas de uma localização já existente.
public record LocalizacaoUpdateDto(decimal X, decimal Y);
