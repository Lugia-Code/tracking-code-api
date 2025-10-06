using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.Builder;
using HealthChecks.UI.Client;
using HealthChecks.UI.Configuration;
using IdempotentAPI.Cache.DistributedCache.Extensions.DependencyInjection;
using IdempotentAPI.Core;
using IdempotentAPI.MinimalAPI;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;
using System.ComponentModel;
using IdempotentAPI.Extensions.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using tracking_code_api;
using System.Text.Json.Serialization;
using tracking_code_api.Dtos;
using tracking_code_api.Dtos.MotoDtos;
using tracking_code_api.Dtos.SetorDtos;
using tracking_code_api.Dtos.TagDtos;
using Microsoft.AspNetCore.Mvc;
using tracking_code_api.Dtos.UsuarioDtos;
using tracking_code_api.Entities;

var builder = WebApplication.CreateBuilder(args);

// Adiciona o serviço para gerenciar o JSON e ignora os ciclos de referência
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.MaxDepth = 32;
});

builder.Services.AddControllers();

builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.MaxDepth = 32;
});
// Configuração do Banco de Dados para Oracle
builder.Services.AddDbContext<MotosDbContext>(opt =>
{
    opt.UseOracle(builder.Configuration.GetConnectionString("FiapOracleDb"));
    
    // IMPORTANTE: Desabilitar o lazy loading para evitar referências circulares
    opt.EnableServiceProviderCaching(false);
    opt.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Idempotent API
builder.Services.AddDistributedMemoryCache();
builder.Services.AddIdempotentMinimalAPI(new IdempotencyOptions());
builder.Services.AddIdempotentAPIUsingDistributedCache();

//builder.Services.AddIdempotentMinimalAPI(new IdempotencyOptions());
//builder.Services.AddDistributedMemoryCache();
//builder.Services.AddIdempotentAPIUsingDistributedCache();

// Health Checks para Oracle
builder.Services.AddHealthChecks()
    .AddOracle(
        connectionString: builder.Configuration.GetConnectionString("FiapOracleDb") ?? string.Empty,
        name: "OracleDb-check",
        tags: new[] { "database", "oracle" },
        failureStatus: HealthStatus.Degraded,
        healthQuery: "SELECT 1 FROM DUAL",
        timeout: TimeSpan.FromSeconds(10)
    );

builder.Services.AddHealthChecksUI(opt =>
{
    opt.SetEvaluationTimeInSeconds(10);
    opt.MaximumHistoryEntriesPerEndpoint(10);
    opt.SetApiMaxActiveRequests(1);
    // CORREÇÃO: Use a URL completa com localhost
    opt.AddHealthCheckEndpoint("motos-api", "http://localhost:5117/health"); 
}).AddInMemoryStorage();

// Rate Limiter
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(60)
            }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync("Muitas requisições, tente novamente em 60 segundos", token);
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin();
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
        policy.WithExposedHeaders("X-Api-Version", "Accept", "Authorization");
    });
});

// Versionamento e OpenAPI
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version")
    );
});

builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer<CircularReferenceSchemaTransformer>();
});

var app = builder.Build();

app.UseRateLimiter();
var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Health endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
})
.WithMetadata(new DisableRateLimitingAttribute());

// HealthChecks UI
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
});


// Mapeamento de Endpoints para suas entidades

// --------------------
// Endpoints de Motos
// --------------------
var motoGroup = app.MapGroup("/api/v1/motos").WithTags("Motos").WithOpenApi();

// Endpoint para obter tags disponíveis para cadastro de moto
motoGroup.MapGet("/tags-disponiveis", async (MotosDbContext db) =>
    {
        var tags = await db.Tags.ToListAsync();
        var tagsLivres = tags.Where(t => string.IsNullOrEmpty(t.Chassi)).ToList();
    
        var resultado = tagsLivres.Select(t => new 
        {
            codigoTag = t.CodigoTag,
            status = t.Status,
            disponivel = true
        }).ToList();

        return Results.Ok(resultado);
    })
.WithSummary("Retorna as tags disponíveis para cadastro de moto")
.WithDescription("Lista todas as tags RFID que não estão vinculadas a nenhuma motocicleta e estão disponíveis para uso.")
.WithOpenApi();

motoGroup.MapGet("/", async (MotosDbContext db, int page = 1, int pageSize = 10) =>
{
    try
    {
        var skip = (page - 1) * pageSize;
        
        var motosData = await db.Motos
            .AsNoTracking()
            .Skip(skip)
            .Take(pageSize)
            .Select(m => new
            {
                Chassi = m.Chassi,
                Placa = m.Placa,
                Modelo = m.Modelo,
                DataCadastro = m.DataCadastro,
                IdSetor = m.IdSetor,
                CodigoTag = m.CodigoTag
            })
            .ToListAsync();

        // Query separada para setores
        var setorIds = motosData.Select(m => m.IdSetor).Distinct().ToList();
        var setoresDict = await db.Setores
            .AsNoTracking()
            .Where(s => setorIds.Contains(s.IdSetor))
            .ToDictionaryAsync(s => s.IdSetor, s => new { s.IdSetor, s.Nome });

        // CORREÇÃO: Query separada para tags - APENAS tags que não são null
        var tagCodes = motosData
            .Where(m => !string.IsNullOrEmpty(m.CodigoTag))  // FILTRAR nulls aqui
            .Select(m => m.CodigoTag!)  // ! indica que sabemos que não é null aqui
            .Distinct()
            .ToList();
            
        var tagsDict = await db.Tags
            .AsNoTracking()
            .Where(t => tagCodes.Contains(t.CodigoTag))
            .ToDictionaryAsync(t => t.CodigoTag, t => new { t.CodigoTag, t.Status, t.DataVinculo });

        // Montar resultado final
        var result = motosData.Select(m => new
        {
            chassi = m.Chassi,
            placa = m.Placa,
            modelo = m.Modelo,
            dataCadastro = m.DataCadastro,
            setor = setoresDict.ContainsKey(m.IdSetor) 
                ? new { idSetor = setoresDict[m.IdSetor].IdSetor, nome = setoresDict[m.IdSetor].Nome }
                : null,
            // CORREÇÃO: Verificar se CodigoTag não é null ANTES de usar ContainsKey
            tag = !string.IsNullOrEmpty(m.CodigoTag) && tagsDict.ContainsKey(m.CodigoTag) 
                ? new { 
                    codigoTag = tagsDict[m.CodigoTag].CodigoTag, 
                    status = tagsDict[m.CodigoTag].Status, 
                    dataVinculo = tagsDict[m.CodigoTag].DataVinculo 
                }
                : null
        }).ToList();

        var totalCount = await db.Motos.CountAsync();
        
        return Results.Ok(new
        {
            data = result,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro: {ex}");
        return Results.Problem("Erro interno do servidor", statusCode: 500);
    }
})
.WithSummary("Retorna todas as motos com paginação")
.WithDescription(@"Lista todas as motocicletas cadastradas no sistema com suporte a paginação.

Parâmetros:
- page: Número da página (padrão: 1)
- pageSize: Quantidade de itens por página (padrão: 10)

Retorna:
- Lista de motos com informações de setor e tag vinculada
- Metadados de paginação (total de itens, páginas, etc.)
- Links HATEOAS para navegação")
.WithOpenApi();

// Endpoint para buscar moto por chassi (se você quiser um específico para busca)
motoGroup.MapGet("/buscar/chassi/{chassi}", async (string chassi, MotosDbContext db) =>
{
    try
    {
        var moto = await db.Motos
            .AsNoTracking()
            .Where(m => m.Chassi == chassi)
            .Select(m => new
            {
                Chassi = m.Chassi,
                Placa = m.Placa,
                Modelo = m.Modelo,
                DataCadastro = m.DataCadastro,
                IdSetor = m.IdSetor,
                CodigoTag = m.CodigoTag
            })
            .FirstOrDefaultAsync();

        if (moto == null)
            return Results.NotFound(new { 
                error = "Moto não encontrada", 
                chassi,
                links = new
                {
                    allMotos = "/api/v1/motos",
                    searchByPlaca = "/api/v1/motos/buscar/placa/{placa}"
                }
            });

        // Buscar setor separadamente
        var setor = await db.Setores
            .AsNoTracking()
            .Where(s => s.IdSetor == moto.IdSetor)
            .Select(s => new { s.IdSetor, s.Nome })
            .FirstOrDefaultAsync();

        // Buscar tag separadamente APENAS se existir CodigoTag
        object? tag = null;
        if (!string.IsNullOrEmpty(moto.CodigoTag))
        {
            var tagData = await db.Tags
                .AsNoTracking()
                .Where(t => t.CodigoTag == moto.CodigoTag)
                .Select(t => new { t.CodigoTag, t.Status, t.DataVinculo })
                .FirstOrDefaultAsync();

            if (tagData != null)
            {
                tag = new
                {
                    codigoTag = tagData.CodigoTag,
                    status = tagData.Status,
                    dataVinculo = tagData.DataVinculo
                };
            }
        }

        var result = new
        {
            chassi = moto.Chassi,
            placa = moto.Placa,
            modelo = moto.Modelo,
            dataCadastro = moto.DataCadastro,
            setor = setor != null ? new
            {
                idSetor = setor.IdSetor,
                nome = setor.Nome
            } : null,
            tag = tag,
            links = new
            {
                self = $"/api/v1/motos/buscar/chassi/{moto.Chassi}",
                motoDetail = $"/api/v1/motos/{moto.Chassi}",
                update = $"/api/v1/motos/{moto.Chassi}",
                delete = $"/api/v1/motos/{moto.Chassi}",
                searchByPlaca = moto.Placa != null ? $"/api/v1/motos/buscar/placa/{moto.Placa}" : null
            }
        };

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro interno: {ex.Message}", statusCode: 500);
    }
})
.WithSummary("Busca uma moto pelo chassi")
.WithDescription("Retorna os detalhes completos de uma motocicleta através do número do chassi.")
.WithOpenApi();

// Endpoint para buscar moto por placa
motoGroup.MapGet("/buscar/placa/{placa}", async (string placa, MotosDbContext db) =>
{
    try
    {
        var moto = await db.Motos
            .AsNoTracking()
            .Where(m => m.Placa == placa)
            .Select(m => new
            {
                Chassi = m.Chassi,
                Placa = m.Placa,
                Modelo = m.Modelo,
                DataCadastro = m.DataCadastro,
                IdSetor = m.IdSetor,
                CodigoTag = m.CodigoTag
            })
            .FirstOrDefaultAsync();

        if (moto == null)
            return Results.NotFound(new { 
                error = "Moto não encontrada", 
                placa,
                links = new
                {
                    allMotos = "/api/v1/motos",
                    searchByChassi = "/api/v1/motos/buscar/chassi/{chassi}"
                }
            });

        // Buscar setor separadamente
        var setor = await db.Setores
            .AsNoTracking()
            .Where(s => s.IdSetor == moto.IdSetor)
            .Select(s => new { s.IdSetor, s.Nome })
            .FirstOrDefaultAsync();

        // Buscar tag separadamente APENAS se existir CodigoTag
        object? tag = null;
        if (!string.IsNullOrEmpty(moto.CodigoTag))
        {
            var tagData = await db.Tags
                .AsNoTracking()
                .Where(t => t.CodigoTag == moto.CodigoTag)
                .Select(t => new { t.CodigoTag, t.Status, t.DataVinculo })
                .FirstOrDefaultAsync();

            if (tagData != null)
            {
                tag = new
                {
                    codigoTag = tagData.CodigoTag,
                    status = tagData.Status,
                    dataVinculo = tagData.DataVinculo
                };
            }
        }

        var result = new
        {
            chassi = moto.Chassi,
            placa = moto.Placa,
            modelo = moto.Modelo,
            dataCadastro = moto.DataCadastro,
            setor = setor != null ? new
            {
                idSetor = setor.IdSetor,
                nome = setor.Nome
            } : null,
            tag = tag,
            links = new
            {
                self = $"/api/v1/motos/buscar/placa/{moto.Placa}",
                motoDetail = $"/api/v1/motos/{moto.Chassi}",
                searchByChassi = $"/api/v1/motos/buscar/chassi/{moto.Chassi}",
                update = $"/api/v1/motos/{moto.Chassi}",
                delete = $"/api/v1/motos/{moto.Chassi}"
            }
        };

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro interno: {ex.Message}", statusCode: 500);
    }
})
.WithSummary("Busca uma moto pela placa")
.WithDescription("Retorna os detalhes completos de uma motocicleta através da placa.")
.WithOpenApi();

motoGroup.MapPost("/", async (MotoCreateDto motoDto, MotosDbContext db) =>
{
    using var transaction = await db.Database.BeginTransactionAsync();
    try 
    {
        // Validação 1: Chassi duplicado
        var chassiExistente = await db.Motos.FirstOrDefaultAsync(m => m.Chassi == motoDto.Chassi);
        if (chassiExistente != null)
        {
            return Results.BadRequest(new { erro = "Já existe uma moto com este chassi", campo = "chassi" });
        }

        // Validação 2: Placa duplicada (se fornecida)
        if (!string.IsNullOrEmpty(motoDto.Placa))
        {
            var placaExistente = await db.Motos.FirstOrDefaultAsync(m => m.Placa == motoDto.Placa);
            if (placaExistente != null)
            {
                return Results.BadRequest(new { erro = "Já existe uma moto com esta placa", campo = "placa" });
            }
        }

        // Validação 3: Setor existe
        var setor = await db.Setores.FindAsync(motoDto.IdSetor);
        if (setor == null)
        {
            return Results.BadRequest(new { erro = "Setor não encontrado", campo = "idSetor" });
        }

        // Validação 4: Tag existe e está disponível
        var tag = await db.Tags.FindAsync(motoDto.CodigoTag);
        if (tag == null)
        {
            return Results.BadRequest(new { erro = "Tag não encontrada", campo = "codigoTag" });
        }
        
        if (!tag.EstaDisponivel)
        {
            return Results.BadRequest(new { 
                erro = "Esta tag já está vinculada a outra moto", 
                campo = "codigoTag",
                chassiVinculado = tag.Chassi 
            });
        }
        
        // Criar a moto
        var moto = new Moto
        {
            Chassi = motoDto.Chassi,
            Placa = motoDto.Placa,
            Modelo = motoDto.Modelo,
            IdSetor = motoDto.IdSetor,
            CodigoTag = motoDto.CodigoTag,
            DataCadastro = DateTime.Now
        };

        // CORREÇÃO PRINCIPAL: Modificar a tag diretamente e marcar como modificada
        tag.VincularMoto(moto.Chassi);
        
        // Marcar explicitamente que a tag foi modificada
        db.Entry(tag).State = EntityState.Modified;
        
        // Adicionar a moto
        db.Motos.Add(moto);
        
        // Salvar todas as mudanças
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        
        // VERIFICAÇÃO: Recarregar a tag para confirmar o status
        var tagAtualizada = await db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.CodigoTag == motoDto.CodigoTag);
        
        // Log para debug
        Console.WriteLine($"Tag após salvar - Código: {tagAtualizada?.CodigoTag}, Status: {tagAtualizada?.Status}, Chassi: {tagAtualizada?.Chassi}");
        
        var motoResponse = new {
            chassi = moto.Chassi,
            placa = moto.Placa,
            modelo = moto.Modelo,
            dataCadastro = moto.DataCadastro,
            setor = new {
                idSetor = setor.IdSetor,
                nome = setor.Nome
            },
            tag = new {
                codigoTag = tagAtualizada?.CodigoTag ?? tag.CodigoTag,
                status = tagAtualizada?.Status ?? tag.Status,
                dataVinculo = tagAtualizada?.DataVinculo ?? tag.DataVinculo,
                chassi = tagAtualizada?.Chassi ?? tag.Chassi
            }
        };

        // VERSÃO COM HATEOAS
        var hateoasResponse = HateoasHelper.AddMotoLinks(moto.Chassi, motoResponse);
        return Results.Created($"/api/v1/motos/{moto.Chassi}", hateoasResponse);

    }
    catch (ArgumentException ex)
    {
        await transaction.RollbackAsync();
        return Results.BadRequest(new { erro = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        await transaction.RollbackAsync();
        return Results.BadRequest(new { erro = ex.Message });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        Console.WriteLine($"Erro completo: {ex}");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Erro interno do servidor"
        );
    }
})
.WithSummary("Cadastra uma nova moto e ativa automaticamente a tag associada")
.WithDescription(@"Cria uma nova motocicleta no sistema e vincula uma tag RFID para rastreamento.

Regras de negócio:
- O chassi deve ser único no sistema
- A placa deve ser única (se fornecida)
- A tag deve existir e estar disponível (não vinculada a outra moto)
- O setor deve existir no sistema
- Ao criar a moto, a tag é automaticamente ativada e vinculada

Validações:
- Chassi: obrigatório, único
- Placa: opcional, mas se fornecida deve ser única
- Modelo: obrigatório
- IdSetor: obrigatório, deve existir
- CodigoTag: obrigatório, deve existir e estar disponível

Exemplo de payload:
{
  'chassi': '9BWZZZ377VT004251',
  'placa': 'ABC1234',
  'modelo': 'Honda CG 160 Fan',
  'idSetor': 1,
  'codigoTag': 'TAG001'
}")
.WithOpenApi();

motoGroup.MapPut("/{chassi}", async (string chassi, MotoUpdateDto updatedMoto, MotosDbContext db) =>
{
    using var transaction = await db.Database.BeginTransactionAsync();
    
    try
    {
        var existingMoto = await db.Motos
            .Include(m => m.Tag)
            .FirstOrDefaultAsync(m => m.Chassi == chassi);
            
        if (existingMoto == null)
            return Results.NotFound(new { erro = "Moto não encontrada" });

        // Verificar duplicata de placa
        if (!string.IsNullOrEmpty(updatedMoto.Placa) && updatedMoto.Placa != existingMoto.Placa)
        {
            var placaDuplicada = await db.Motos.FirstOrDefaultAsync(m => m.Placa == updatedMoto.Placa && m.Chassi != chassi);
            if (placaDuplicada != null)
            {
                return Results.BadRequest(new { erro = "Já existe uma moto com esta placa", campo = "placa" });
            }
        }

        // Atualizar setor
        if (updatedMoto.IdSetor.HasValue && updatedMoto.IdSetor != existingMoto.IdSetor)
        {
            var setor = await db.Setores.FindAsync(updatedMoto.IdSetor);
            if (setor == null)
            {
                return Results.BadRequest(new { erro = "Setor não encontrado", campo = "idSetor" });
            }
            existingMoto.IdSetor = updatedMoto.IdSetor.Value;
        }

        // Atualizar outros campos
        if (!string.IsNullOrEmpty(updatedMoto.Placa))
            existingMoto.Placa = updatedMoto.Placa;
        
        if (!string.IsNullOrEmpty(updatedMoto.Modelo))
            existingMoto.Modelo = updatedMoto.Modelo;

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return Results.Ok(new { 
            message = "Moto atualizada com sucesso",
            chassi = existingMoto.Chassi,
            links = new
            {
                self = $"/api/v1/motos/{chassi}",
                vincularTag = $"/api/v1/motos/{chassi}/tag",
                desvincularTag = $"/api/v1/motos/{chassi}/tag",
                tagsDisponiveis = "/api/v1/tags/disponiveis"
            }
        });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return Results.Problem($"Erro interno: {ex.Message}", statusCode: 500);
    }
})
.WithSummary("Atualiza dados básicos da moto (placa, modelo, setor) - use endpoints específicos para gerenciar tags")
.WithDescription(@"Atualiza informações básicas de uma motocicleta existente.

Campos atualizáveis:
- Placa (deve ser única)
- Modelo
- IdSetor (deve existir)

Nota: Para gerenciar tags, use os endpoints específicos:
- PUT /api/v1/motos/{chassi}/tag - para vincular nova tag
- PATCH /api/v1/motos/{chassi}/desvincular-tag - para desvincular tag

Exemplo de payload:
{
  'placa': 'XYZ9876',
  'modelo': 'Honda CG 160 Start',
  'idSetor': 2
}")
.WithOpenApi();

motoGroup.MapDelete("/{chassi}", async (string chassi, MotosDbContext db) =>
    {
        using var transaction = await db.Database.BeginTransactionAsync();
    
        try
        {
            var moto = await db.Motos
                .Include(m => m.Tag)
                .FirstOrDefaultAsync(m => m.Chassi == chassi);
            
            if (moto == null)
                return Results.NotFound(new { erro = "Moto não encontrada" });

            // Desvincular a tag automaticamente (desativa ela)
            if (moto.Tag != null)
            {
                moto.Tag.DesvincularMoto();
            }

            db.Motos.Remove(moto);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Erro interno do servidor"
            );
        }
    })
    .WithSummary("Remove uma moto e desativa automaticamente a tag associada")
    .WithDescription(@"Remove permanentemente uma motocicleta do sistema.

        Ação automática:
        - Se a moto possuir uma tag vinculada, ela será automaticamente desvinculada e desativada
        - A tag ficará disponível para ser vinculada a outra moto")
    .WithOpenApi();

// Endpoint para vincular nova tag a uma moto
motoGroup.MapPut("/{chassi}/tag", async (string chassi, [FromBody] VincularTagDto vincularDto, MotosDbContext db) =>
    {
        using var transaction = await db.Database.BeginTransactionAsync();
    
        try
        {
            var moto = await db.Motos
                .Include(m => m.Tag)
                .FirstOrDefaultAsync(m => m.Chassi == chassi);
        
            if (moto == null)
                return Results.NotFound(new { erro = "Moto não encontrada", chassi });

            // Verificar se a nova tag existe e está disponível
            var novaTag = await db.Tags.FindAsync(vincularDto.CodigoTag);
            if (novaTag == null)
                return Results.BadRequest(new { erro = "Tag não encontrada", codigoTag = vincularDto.CodigoTag });

            if (!novaTag.EstaDisponivel)
            {
                return Results.BadRequest(new { 
                    erro = "Tag já está vinculada a outra moto", 
                    codigoTag = vincularDto.CodigoTag,
                    chassiVinculado = novaTag.Chassi
                });
            }

            // Desvincular tag atual se existir
            if (moto.Tag != null)
            {
                moto.Tag.DesvincularMoto();
            }

            // Vincular nova tag
            novaTag.VincularMoto(chassi);
            moto.CodigoTag = vincularDto.CodigoTag;
        
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Results.Ok(new { 
                message = "Tag vinculada com sucesso",
                chassi = moto.Chassi,
                novaTag = vincularDto.CodigoTag,
                links = new
                {
                    moto = $"/api/v1/motos/{chassi}",
                    desvincularTag = $"/api/v1/motos/{chassi}/tag"
                }
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Results.Problem($"Erro interno: {ex.Message}", statusCode: 500);
        }
    })
    .WithSummary("Vincula uma nova tag a uma moto")
    .WithDescription(@"Vincula ou substitui a tag RFID de uma motocicleta.
    Regras:
    - A tag deve existir e estar disponível
    - Se a moto já possuir uma tag, ela será automaticamente desvinculada
    - A nova tag será ativada e vinculada à moto

    Exemplo de payload:
    {
    'codigoTag': 'TAG002'
    }")
    .WithOpenApi();

motoGroup.MapPatch("/{chassi}/desvincular-tag", async (string chassi, MotosDbContext db) =>
    {
        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var moto = await db.Motos
                .Include(m => m.Tag)
                .FirstOrDefaultAsync(m => m.Chassi == chassi);

            if (moto == null)
                return Results.NotFound(new { erro = "Moto não encontrada", chassi });

            if (string.IsNullOrEmpty(moto.CodigoTag) || moto.Tag == null)
                return Results.BadRequest(new { erro = "Esta moto já não possui tag vinculada" });

            // Guardar informação da tag antes de desvincular
            var tagAnterior = moto.CodigoTag;

            moto.Tag.DesvincularMoto();

            db.Entry(moto.Tag).State = EntityState.Modified;

            moto.CodigoTag = null;

            db.Entry(moto).State = EntityState.Modified;

            // Salvar mudanças
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            // VERIFICAÇÃO: Recarregar para confirmar as mudanças
            var motoVerificacao = await db.Motos
                .AsNoTracking()
                .Where(m => m.Chassi == chassi)
                .Select(m => new { m.Chassi, m.CodigoTag })
                .FirstOrDefaultAsync();

            var tagVerificacao = await db.Tags
                .AsNoTracking()
                .Where(t => t.CodigoTag == tagAnterior)
                .Select(t => new { t.CodigoTag, t.Status, t.Chassi })
                .FirstOrDefaultAsync();

            // Log para debug
            Console.WriteLine($"Verificação após desvinculação:");
            Console.WriteLine($"Moto {chassi} - CodigoTag: {motoVerificacao?.CodigoTag ?? "NULL"}");
            Console.WriteLine(
                $"Tag {tagAnterior} - Status: {tagVerificacao?.Status}, Chassi: {tagVerificacao?.Chassi ?? "NULL"}");

            return Results.Ok(new
            {
                message = "Tag desvinculada com sucesso",
                chassi = moto.Chassi,
                tagAnterior = tagAnterior,
                // Dados de verificação (remova em produção se não precisar)
                verificacao = new
                {
                    motoCodigoTag = motoVerificacao?.CodigoTag,
                    tagStatus = tagVerificacao?.Status,
                    tagChassi = tagVerificacao?.Chassi
                },
                links = new
                {
                    moto = $"/api/v1/motos/{chassi}",
                    vincularNovaTag = $"/api/v1/motos/{chassi}/tag",
                    tagsDisponiveis = "/api/v1/tags/disponiveis"
                }
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Erro ao desvincular tag: {ex}");
            return Results.Problem($"Erro interno: {ex.Message}", statusCode: 500);
        }
    })
    .WithSummary("Desvincula a tag de uma moto")
    .WithDescription(@"Remove o vínculo entre uma motocicleta e sua tag RFID.
    Ação realizada:
    - A tag é desvinculada e desativada
    - A moto fica sem tag associada
    - A tag volta a ficar disponível para vinculação")
    .WithOpenApi();

motoGroup.MapGet("/setor/{idSetor}", async (int idSetor, MotosDbContext db, int page = 1, int pageSize = 10) =>
{
    try
    {
        // Verificar se o setor existe
        var setor = await db.Setores.AsNoTracking().FirstOrDefaultAsync(s => s.IdSetor == idSetor);
        if (setor == null)
        {
            return Results.NotFound(new { erro = "Setor não encontrado", idSetor });
        }

        var skip = (page - 1) * pageSize;
        
        // CORREÇÃO: Não usar Include() - fazer queries separadas para evitar nullable errors
        var motosData = await db.Motos
            .AsNoTracking()
            .Where(m => m.IdSetor == idSetor)
            .Skip(skip)
            .Take(pageSize)
            .Select(m => new
            {
                Chassi = m.Chassi,
                Placa = m.Placa,
                Modelo = m.Modelo,
                DataCadastro = m.DataCadastro,
                IdSetor = m.IdSetor,
                CodigoTag = m.CodigoTag
            })
            .ToListAsync();

        // Se não há motos neste setor, retornar resposta vazia mas válida
        if (!motosData.Any())
        {
            var totalCount = await db.Motos.CountAsync(m => m.IdSetor == idSetor);
            
            return Results.Ok(new {
                setor = new {
                    idSetor = setor.IdSetor,
                    nome = setor.Nome
                },
                data = new List<object>(), // Lista vazia
                pagination = new {
                    page,
                    pageSize,
                    totalCount = 0,
                    totalPages = 0
                },
                message = "Nenhuma moto encontrada neste setor"
            });
        }

        // Buscar tags apenas para motos que têm CodigoTag não nulo
        var tagCodes = motosData
            .Where(m => !string.IsNullOrEmpty(m.CodigoTag))
            .Select(m => m.CodigoTag!)
            .Distinct()
            .ToList();

        var tagsDict = new Dictionary<string, object>();
        if (tagCodes.Any())
        {
            tagsDict = await db.Tags
                .AsNoTracking()
                .Where(t => tagCodes.Contains(t.CodigoTag))
                .ToDictionaryAsync(
                    t => t.CodigoTag, 
                    t => new {
                        codigoTag = t.CodigoTag,
                        status = t.Status,
                        dataVinculo = t.DataVinculo,
                        chassi = t.Chassi
                    } as object
                );
        }

        // Montar resultado
        var motos = motosData.Select(m => new {
            chassi = m.Chassi,
            placa = m.Placa,
            modelo = m.Modelo,
            dataCadastro = m.DataCadastro,
            setor = new {
                idSetor = setor.IdSetor,
                nome = setor.Nome
            },
            tag = !string.IsNullOrEmpty(m.CodigoTag) && tagsDict.ContainsKey(m.CodigoTag)
                ? tagsDict[m.CodigoTag]
                : null
        }).ToList();

        var totalCountFinal = await db.Motos.CountAsync(m => m.IdSetor == idSetor);
        
        var response = new {
            setor = new {
                idSetor = setor.IdSetor,
                nome = setor.Nome
            },
            data = motos,
            pagination = new {
                page,
                pageSize,
                totalCount = totalCountFinal,
                totalPages = (int)Math.Ceiling((double)totalCountFinal / pageSize)
            }
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        // Log detalhado para debug
        Console.WriteLine($"Erro no setor {idSetor}: {ex}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Erro interno do servidor"
        );
    }
})
.WithSummary("Retorna motos de um setor específico com paginação")
.WithDescription(@"Lista todas as motocicletas de um determinado setor.

Parâmetros:
- idSetor: ID do setor
- page: Número da página (padrão: 1)
- pageSize: Quantidade de itens por página (padrão: 10)")
.WithOpenApi();

// Endpoints de Tags (CRUD
// GET /api/v1/tags
var tagGroup = app.MapGroup("/api/v1/tags").WithTags("Tags").WithOpenApi();

tagGroup.MapGet("/", async (MotosDbContext db) =>
{
    var tags = await db.Tags
        .AsNoTracking()
        .Select(t => new {
            codigoTag = t.CodigoTag,
            status = t.Status,
            dataVinculo = t.DataVinculo,
            chassi = t.Chassi
        })
        .ToListAsync();
    
    return Results.Ok(tags);
})
.WithSummary("Retorna todas as tags")
.WithDescription("Lista todas as tags RFID cadastradas no sistema, independente do status.")
.WithOpenApi()
.Produces<object[]>(200); // Tipo explícito

tagGroup.MapGet("/{codigo}", async (string codigo, MotosDbContext db) =>
{
    var tag = await db.Tags
        .AsNoTracking()
        .Where(t => t.CodigoTag == codigo)
        .Select(t => new {
            codigoTag = t.CodigoTag,
            status = t.Status,
            dataVinculo = t.DataVinculo,
            chassi = t.Chassi
        })
        .FirstOrDefaultAsync();

    return tag != null ? Results.Ok(tag) : Results.NotFound(new { erro = "Tag não encontrada" });
})
.WithSummary("Retorna uma TAG pelo código")
.WithDescription("Busca os detalhes de uma TAG específica através do seu código.")
.WithOpenApi()
.Produces<object>(200)
.Produces<object>(404);

tagGroup.MapPost("/", async ([FromBody] TagCreateDto tagDto, MotosDbContext db) =>
{
    try
    {
        // Verificar se já existe
        var existente = await db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.CodigoTag == tagDto.CodigoTag);
        if (existente != null)
        {
            return Results.BadRequest(new { erro = "Já existe uma tag com este código", campo = "codigoTag" });
        }

        // Criar nova tag
        var novaTag = new Tag
        {
            CodigoTag = tagDto.CodigoTag,
            Status = "inativo",
            DataVinculo = DateTime.Now,
            Chassi = null
        };
        
        db.Tags.Add(novaTag);
        await db.SaveChangesAsync();

        // Retornar objeto anônimo
        var response = new {
            codigoTag = novaTag.CodigoTag,
            status = novaTag.Status,
            dataVinculo = novaTag.DataVinculo,
            chassi = novaTag.Chassi
        };

        return Results.Created($"/api/v1/tags/{novaTag.CodigoTag}", response);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { erro = ex.Message });
    }
})
.WithSummary("Cria uma nova tag")
.WithDescription(@"Cadastra uma nova tag RFID no sistema.

Por padrão, tags são criadas com status 'inativo'.
A tag só será ativada quando vinculada a uma moto.

Exemplo de payload:
{
  'codigoTag': 'TAG003'
}")
.WithOpenApi()
.Accepts<TagCreateDto>("application/json")
.Produces<object>(201)
.Produces<object>(400);

tagGroup.MapPut("/{codigo}", async (string codigo, [FromBody] TagUpdateDto tagUpdate, MotosDbContext db) =>
{
    try
    {
        var existingTag = await db.Tags.FindAsync(codigo);
        if (existingTag == null)
            return Results.NotFound(new { erro = "Tag não encontrada" });

        // Só permite alterar status se não estiver vinculada a uma moto
        if (string.IsNullOrEmpty(existingTag.Chassi) && !string.IsNullOrEmpty(tagUpdate.Status))
        {
            existingTag.Status = tagUpdate.Status;
            await db.SaveChangesAsync();
        }
        else if (!string.IsNullOrEmpty(existingTag.Chassi))
        {
            return Results.BadRequest(new { erro = "Não é possível alterar status de tag vinculada a uma moto" });
        }

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { erro = ex.Message });
    }
})
.WithSummary("Atualiza uma tag existente")
.WithDescription(@"Atualiza o status de uma tag RFID.

Restrições:
- Só é possível alterar tags que não estejam vinculadas a motos
- Para alterar tags vinculadas, primeiro desvincule da moto

Exemplo de payload:
{
  'status': 'inativo'
}")
.WithOpenApi()
.Accepts<TagUpdateDto>("application/json")
.Produces(204)
.Produces<object>(400)
.Produces<object>(404);

tagGroup.MapDelete("/{codigo}", async (string codigo, MotosDbContext db) =>
{
    try
    {
        var tag = await db.Tags.FindAsync(codigo);
        if (tag == null)
            return Results.NotFound(new { erro = "Tag não encontrada" });

        // Só permite deletar se não estiver vinculada a uma moto
        if (!string.IsNullOrEmpty(tag.Chassi))
        {
            return Results.BadRequest(new { erro = "Não é possível deletar uma tag vinculada a uma moto" });
        }

        db.Tags.Remove(tag);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { erro = ex.Message });
    }
})
.WithSummary("Deleta uma tag")
.WithDescription(@"Remove permanentemente uma tag do sistema.

Restrição:
- Só é possível deletar tags que não estejam vinculadas a motos")
.WithOpenApi()
.Produces(204)
.Produces<object>(400)
.Produces<object>(404);


// CRUD para a Entidade Usuario
var usuarioGroup = app.MapGroup("/usuarios").WithTags("Usuarios");

usuarioGroup.MapGet("/", async (MotosDbContext db) =>
    {
        var usuarios = await db.Usuarios.ToListAsync();
        var usuariosDto = usuarios.Select(u => new UsuarioReadDto(u.IdFuncionario, u.Email, u.Funcao)).ToList();
        return Results.Ok(usuariosDto);
    })
    .WithSummary("Retorna todos os usuários.")
    .WithDescription("Lista todos os usuários cadastrados no sistema.");


usuarioGroup.MapGet("/{id}", async (int id, MotosDbContext db) =>
    {
        var usuario = await db.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return Results.NotFound();
        }
    
        var usuarioDto = new UsuarioReadDto(usuario.IdFuncionario, usuario.Email, usuario.Funcao);
        return Results.Ok(usuarioDto);
    })
    .WithSummary("Retorna um usuário pelo ID.")
    .WithDescription("Busca os detalhes de um usuário específico através do ID.");

usuarioGroup.MapPost("/", async (UsuarioCreateDto usuarioDto, MotosDbContext db) =>
    {
        // Mapeia o DTO de entrada para a entidade de domínio
        var usuario = new Usuario
        {
            Email = usuarioDto.Email,
            Senha = usuarioDto.Senha,
            Funcao = usuarioDto.Papel
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
    
        // Mapeia a entidade criada para o DTO de retorno
        var usuarioRetornoDto = new UsuarioReadDto(usuario.IdFuncionario, usuario.Email, usuario.Funcao);
    
        return Results.Created($"/usuarios/{usuario.IdFuncionario}", usuarioRetornoDto);
    })
    .WithSummary("Cria um novo usuário.")
    .WithDescription(@"Cadastra um novo usuário no sistema.

    Exemplo de payload:
    {   
    'email': 'joao.silva@empresa.com',
    'senha': 'SenhaSegura@123',
    'papel': 'Admin'
    }")
    .AddEndpointFilter<IdempotentAPIEndpointFilter>();

usuarioGroup.MapPut("/{id}", async (int id, UsuarioUpdateDto usuarioDto, MotosDbContext db) =>
    {
        var existingUsuario = await db.Usuarios.FindAsync(id);
        if (existingUsuario == null)
            return Results.NotFound();

        // Aplica as atualizações apenas se o valor não for nulo
        if (usuarioDto.Email != null) existingUsuario.Email = usuarioDto.Email;
        if (usuarioDto.Senha != null) existingUsuario.Senha = usuarioDto.Senha;
        if (usuarioDto.Papel != null) existingUsuario.Funcao = usuarioDto.Papel;
    
        await db.SaveChangesAsync();
        return Results.NoContent();
    })
    .WithSummary("Atualiza um usuário existente.")
    .WithDescription(@"Atualiza as informações de um usuário.

    Todos os campos são opcionais - informe apenas os que deseja atualizar.

    Exemplo de payload:
    {
    'email': 'novo.email@empresa.com',
    'senha': 'NovaSenha@456',
    'papel': 'Operador'
    }");

usuarioGroup.MapDelete("/{id}", async (int id, MotosDbContext db) =>
    {
        var usuario = await db.Usuarios.FindAsync(id);
        if (usuario == null)
            return Results.NotFound();

        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync();
        return Results.NoContent();
    })
    .WithSummary("Deleta um usuário.")
    .WithDescription("Remove permanentemente um usuário do sistema.");


// CRUD para a Entidade Setor
var setorGroup = app.MapGroup("/setores").WithTags("Setores");

setorGroup.MapGet("/", async (MotosDbContext db) =>
    {
        var setores = await db.Setores.ToListAsync();
        var setoresDto = setores.Select(s => new SetorReadDto(s.IdSetor, s.Nome)).ToList();
        return Results.Ok(setoresDto);
    })
    .WithSummary("Retorna todos os setores.")
    .WithDescription("Lista todos os setores cadastrados no sistema.");


setorGroup.MapGet("/{id}", async (int id, MotosDbContext db) =>
    {
        var setor = await db.Setores.FirstOrDefaultAsync(s => s.IdSetor == id);
        if (setor == null)
        {
            return Results.NotFound();
        }
    
        var setorDto = new SetorReadDto(setor.IdSetor, setor.Nome);
        return Results.Ok(setorDto);
    })
    .WithSummary("Retorna um setor pelo ID.")
    .WithDescription("Busca os detalhes de um setor específico através do ID.");

setorGroup.MapPost("/", async (SetorCreateDto setorDto, MotosDbContext db) =>
    {
        var setor = new Setor
        {
            Nome = setorDto.Nome
        };
    
        db.Setores.Add(setor);
        await db.SaveChangesAsync();
    
        var setorRetornoDto = new SetorReadDto(setor.IdSetor, setor.Nome);
    
        return Results.Created($"/setores/{setor.IdSetor}", setorRetornoDto);
    })
    .WithSummary("Cria um novo setor.")
    .WithDescription(@"Cadastra um novo setor no sistema.

    Exemplo de payload:
    {
    'nome': 'Na rua'
    }")
    .AddEndpointFilter<IdempotentAPIEndpointFilter>();

setorGroup.MapPut("/{id}", async (int id, SetorUpdateDto setorDto, MotosDbContext db) =>
    {
        var existingSetor = await db.Setores.FindAsync(id);
        if (existingSetor == null)
            return Results.NotFound();

        if (setorDto.Nome != null) existingSetor.Nome = setorDto.Nome;
    
        await db.SaveChangesAsync();
        return Results.NoContent();
    })
    .WithSummary("Atualiza um setor existente.")
    .WithDescription(@"Atualiza as informações de um setor.

    Exemplo de payload:
    {
    'id_setor': 3
    }");

setorGroup.MapDelete("/{id}", async (int id, MotosDbContext db) =>
    {
        var setor = await db.Setores.FindAsync(id);
        if (setor == null)
            return Results.NotFound();

        db.Setores.Remove(setor);
        await db.SaveChangesAsync();
        return Results.NoContent();
    })
    .WithSummary("Deleta um setor.")
    .WithDescription("Remove permanentemente um setor do sistema.");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<MotosDbContext>();
    //DataSeeder.SeedData(context);
}

app.Run();

