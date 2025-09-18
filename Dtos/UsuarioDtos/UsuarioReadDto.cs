namespace tracking_code_api.Dtos.UsuarioDtos;

// DTO para a resposta dos endpoints GET (leitura)
// Inclui apenas os dados que devem ser expostos publicamente.
public record UsuarioReadDto(int Id, string Email, string Papel);