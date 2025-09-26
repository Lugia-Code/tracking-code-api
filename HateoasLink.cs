using Microsoft.EntityFrameworkCore;
namespace tracking_code_api;
public record HateoasLink(string Href, string Rel, string Method = "GET");

public static class HateoasHelper
{
    public static object AddMotoLinks(string chassi, object motoData)
    {
        var links = new[]
        {
            new HateoasLink($"/api/v1/motos/{chassi}", "self"),
            new HateoasLink($"/api/v1/motos/{chassi}", "update", "PUT"),
            new HateoasLink($"/api/v1/motos/{chassi}", "delete", "DELETE"),
            new HateoasLink("/api/v1/motos", "collection")
        };

        return new { 
            data = motoData, 
            _links = links.ToDictionary(l => l.Rel, l => new { href = l.Href, method = l.Method })
        };
    }

    public static object AddTagLinks(string codigoTag, object tagData)
    {
        var links = new[]
        {
            new HateoasLink($"/api/v1/tags/{codigoTag}", "self"),
            new HateoasLink($"/api/v1/tags/{codigoTag}", "update", "PUT"),
            new HateoasLink($"/api/v1/tags/{codigoTag}", "delete", "DELETE"),
            new HateoasLink("/api/v1/tags", "collection")
        };

        return new { 
            data = tagData, 
            _links = links.ToDictionary(l => l.Rel, l => new { href = l.Href, method = l.Method })
        };
    }
    
    public static object AddUsuarioLinks(int id, object usuarioData)
    {
        var links = new[]
        {
            new HateoasLink($"/usuarios/{id}", "self"),
            new HateoasLink($"/usuarios/{id}", "update", "PUT"),
            new HateoasLink($"/usuarios/{id}", "delete", "DELETE"),
            new HateoasLink("/usuarios", "collection")
        };

        return new { 
            data = usuarioData, 
            _links = links.ToDictionary(l => l.Rel, l => new { href = l.Href, method = l.Method })
        };
    }

    public static object AddSetorLinks(int idSetor, object setorData)
    {
        var links = new[]
        {
            new HateoasLink($"/setores/{idSetor}", "self"),
            new HateoasLink($"/setores/{idSetor}", "update", "PUT"),
            new HateoasLink($"/setores/{idSetor}", "delete", "DELETE"),
            new HateoasLink("/setores", "collection"),
            new HateoasLink($"/api/v1/motos/setor/{idSetor}", "motos")
        };

        return new { 
            data = setorData, 
            _links = links.ToDictionary(l => l.Rel, l => new { href = l.Href, method = l.Method })
        };
    }

    public static object AddCollectionLinks(string basePath, object collectionData, int? page = null, int? pageSize = null, int? totalPages = null)
    {
        var links = new List<HateoasLink>
        {
            new($"{basePath}", "self"),
            new($"{basePath}", "create", "POST")
        };

        // Adicionar links de paginação se aplicável
        if (page.HasValue && totalPages.HasValue)
        {
            if (page > 1)
                links.Add(new($"{basePath}?page={page - 1}&pageSize={pageSize}", "prev"));
            
            if (page < totalPages)
                links.Add(new($"{basePath}?page={page + 1}&pageSize={pageSize}", "next"));
            
            links.Add(new($"{basePath}?page=1&pageSize={pageSize}", "first"));
            links.Add(new($"{basePath}?page={totalPages}&pageSize={pageSize}", "last"));
        }

        return new { 
            data = collectionData, 
            _links = links.ToDictionary(l => l.Rel, l => new { href = l.Href, method = l.Method })
        };
    }
}