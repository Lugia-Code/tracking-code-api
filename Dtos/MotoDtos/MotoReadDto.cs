using tracking_code_api.Dtos.SetorDtos;
using tracking_code_api.Dtos.TagDtos;

namespace tracking_code_api.Dtos.MotoDtos;

// DTO de leitura ajustado para Setor e Tag serem opcionais
public record MotoReadDto(
    string Chassi,
    string? Placa,
    string Modelo,
    DateTime DataCadastro,
    SetorReadDto Setor, // permite nulo
    TagReadDto Tag      // permite nulo
);
