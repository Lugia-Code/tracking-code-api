using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using tracking_code_api.Entities;

namespace tracking_code_api;

public class CircularReferenceSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        
        // Para entidades que têm referências circulares, remover propriedades de navegação do schema
        if (type == typeof(Tag))
        {
            schema.Properties?.Remove("moto");
            schema.Properties?.Remove("localizacoes");
            schema.Properties?.Remove("estaDisponivel");
            schema.Properties?.Remove("estaAtiva");
        }
        
        if (type == typeof(Moto))
        {
            schema.Properties?.Remove("tag");
            schema.Properties?.Remove("setor");
            schema.Properties?.Remove("auditoria");
        }
        
        if (type == typeof(Setor))
        {
            schema.Properties?.Remove("motos");
            schema.Properties?.Remove("localizacoes");
        }
        
        if (type == typeof(Localizacao))
        {
            schema.Properties?.Remove("tag");
            schema.Properties?.Remove("setor");
        }
        
        return Task.CompletedTask;
    }
}