namespace tracking_code_api.Dtos.UsuarioDtos;

// DTO para a requisição de criação (POST)
// Usado para registrar um novo usuário com as informações necessárias.
public record UsuarioCreateDto(string Email, string Senha, string Papel);