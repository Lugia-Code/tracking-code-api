namespace tracking_code_api.Dtos.UsuarioDtos;

// DTO para a requisição de atualização (PUT)
// Permite que o usuário atualize suas informações.
public record UsuarioUpdateDto(string Email, string Senha, string Papel);