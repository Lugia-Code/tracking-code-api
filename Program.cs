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


var builder = WebApplication.CreateBuilder(args);

// Adiciona o serviço para gerenciar o JSON e ignora os ciclos de referência
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

// Configuração do Banco de Dados para Oracle
builder.Services.AddDbContext<MotosDbContext>(opt
    => opt.UseOracle(builder.Configuration.GetConnectionString("FiapOracleDb")));
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
    opt.AddHealthCheckEndpoint("motos-api", "/health");
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
builder.Services.AddOpenApi();

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
.WithOpenApi();

motoGroup.MapGet("/", async (MotosDbContext db, int page = 1, int pageSize = 10) =>
{
    var skip = (page - 1) * pageSize;
    var motos = await db.Motos
        .Include(m => m.Setor)
        .Include(m => m.Tag)
        .Skip(skip)
        .Take(pageSize)
        .ToListAsync();

    var motosDto = motos.Select(m => new MotoReadDto(
        m.Chassi,
        m.Placa,
        m.Modelo,
        m.DataCadastro,
        new SetorReadDto(m.Setor.IdSetor, m.Setor.Nome),
        new TagReadDto(m.Tag.CodigoTag, m.Tag.Status, m.Tag.DataVinculo, m.Tag.Chassi)
    )).ToList();

    var totalCount = await db.Motos.CountAsync();
    
    return Results.Ok(new
    {
        data = motosDto,
        pagination = new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        }
    });
})
.WithSummary("Retorna todas as motos com paginação")
.WithOpenApi();

motoGroup.MapGet("/{chassi}", async (string chassi, MotosDbContext db) =>
{
    var moto = await db.Motos
        .Include(m => m.Setor)
        .Include(m => m.Tag)
        .FirstOrDefaultAsync(m => m.Chassi == chassi);

    if (moto == null)
        return Results.NotFound();

    var motoDto = new MotoReadDto(
        moto.Chassi,
        moto.Placa,
        moto.Modelo,
        moto.DataCadastro,
        new SetorReadDto(moto.Setor.IdSetor, moto.Setor.Nome),
        new TagReadDto(moto.Tag.CodigoTag, moto.Tag.Status, moto.Tag.DataVinculo, moto.Tag.Chassi)
    );

    return Results.Ok(motoDto);
})
.WithSummary("Retorna uma moto pelo chassi")
.WithOpenApi();

motoGroup.MapPost("/", async (MotoCreateDto motoDto, MotosDbContext db) =>
    {
        try 
        {
            // Validação 1: Chassi duplicado
            var chassiExistente = await db.Motos.FirstOrDefaultAsync(m => m.Chassi == motoDto.Chassi);
            if (chassiExistente != null)
            {
                return Results.BadRequest("Já existe uma moto com este chassi");
            }

            // Validação 2: Placa duplicada (se fornecida)
            if (!string.IsNullOrEmpty(motoDto.Placa))
            {
                var placaExistente = await db.Motos.FirstOrDefaultAsync(m => m.Placa == motoDto.Placa);
                if (placaExistente != null)
                {
                    return Results.BadRequest("Já existe uma moto com esta placa");
                }
            }

            // Validação 3: Setor existe
            var setor = await db.Setores.FindAsync(motoDto.IdSetor);
            if (setor == null)
            {
                return Results.BadRequest("Setor não encontrado");
            }

            // Validação 4: Tag existe e está disponível
            var tag = await db.Tags.FindAsync(motoDto.CodigoTag);
            if (tag == null)
            {
                return Results.BadRequest("Tag não encontrada");
            }

            if (!string.IsNullOrEmpty(tag.Chassi))
            {
                return Results.BadRequest("Esta tag já está vinculada a outra moto");
            }

            var moto = new Moto
            {
                Chassi = motoDto.Chassi,
                Placa = motoDto.Placa,
                Modelo = motoDto.Modelo,
                IdSetor = motoDto.IdSetor,
                CodigoTag = motoDto.CodigoTag,
                DataCadastro = DateTime.Now
            };

            // Ativar a tag
            tag.VincularMoto(moto.Chassi);

            db.Motos.Add(moto);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/motos/{moto.Chassi}", moto);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ex.Message);
        }
    })
    .WithSummary("Cadastra uma nova moto")
    .WithOpenApi();

motoGroup.MapPut("/{chassi}", async (string chassi, MotoUpdateDto updatedMoto, MotosDbContext db) =>
{
    try
    {
        var existingMoto = await db.Motos.Include(m => m.Tag).FirstOrDefaultAsync(m => m.Chassi == chassi);
        if (existingMoto == null)
            return Results.NotFound();

        // Verificar duplicata de placa
        if (!string.IsNullOrEmpty(updatedMoto.Placa) && updatedMoto.Placa != existingMoto.Placa)
        {
            var placaDuplicada = await db.Motos.FirstOrDefaultAsync(m => m.Placa == updatedMoto.Placa && m.Chassi != chassi);
            if (placaDuplicada != null)
            {
                return Results.BadRequest("Já existe uma moto com esta placa");
            }
        }

        // Verificar se o setor existe
        if (updatedMoto.IdSetor.HasValue && updatedMoto.IdSetor != existingMoto.IdSetor)
        {
            var setor = await db.Setores.FindAsync(updatedMoto.IdSetor);
            if (setor == null)
            {
                return Results.BadRequest("Setor não encontrado");
            }
            existingMoto.IdSetor = updatedMoto.IdSetor.Value;
        }

        // Verificar troca de tag
        if (!string.IsNullOrEmpty(updatedMoto.CodigoTag) && updatedMoto.CodigoTag != existingMoto.CodigoTag)
        {
            var newTag = await db.Tags.FindAsync(updatedMoto.CodigoTag);
            if (newTag == null)
            {
                return Results.BadRequest("Nova tag não encontrada");
            }

            // Verificar se a nova tag está livre
            if (!string.IsNullOrEmpty(newTag.Chassi))
            {
                return Results.BadRequest("A nova tag já está vinculada a outra moto");
            }

            // Desativar tag antiga
            if (existingMoto.Tag != null)
            {
                existingMoto.Tag.Status = "inativo";
                existingMoto.Tag.Chassi = null;
            }

            // Ativar nova tag
            newTag.VincularMoto(chassi);
            existingMoto.CodigoTag = updatedMoto.CodigoTag;
        }

        // Atualizar outros campos
        if (!string.IsNullOrEmpty(updatedMoto.Placa))
            existingMoto.Placa = updatedMoto.Placa;
        
        if (!string.IsNullOrEmpty(updatedMoto.Modelo))
            existingMoto.Modelo = updatedMoto.Modelo;

        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
})
.WithSummary("Atualiza uma moto existente")
.WithOpenApi();

motoGroup.MapDelete("/{chassi}", async (string chassi, MotosDbContext db) =>
{
    var moto = await db.Motos.Include(m => m.Tag).FirstOrDefaultAsync(m => m.Chassi == chassi);
    if (moto == null)
        return Results.NotFound();

    // Desativar a tag associada quando a moto for removida
    if (moto.Tag != null)
    {
        moto.Tag.Status = "inativo";
        moto.Tag.Chassi = null;
    }

    db.Motos.Remove(moto);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithSummary("Remove uma moto e libera a tag associada")
.WithOpenApi();


// CRUD para a Entidade Usuario
var usuarioGroup = app.MapGroup("/usuarios").WithTags("Usuarios");

usuarioGroup.MapGet("/", async (MotosDbContext db) =>
        await db.Usuarios.ToListAsync())
    .WithSummary("Retorna todos os usuários.");

usuarioGroup.MapGet("/{id}", async (int id, MotosDbContext db) =>
        await db.Usuarios.FindAsync(id) is { } usuario
            ? Results.Ok(usuario)
            : Results.NotFound())
    .WithSummary("Retorna um usuário pelo ID.");

usuarioGroup.MapPost("/", async (Usuario usuario, MotosDbContext db) =>
    {
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return Results.Created($"/usuarios/{usuario.IdFuncionario}", usuario);
    })
    .WithSummary("Cria um novo usuário.")
    .AddEndpointFilter<IdempotentAPIEndpointFilter>();

usuarioGroup.MapPut("/{id}", async (int id, Usuario usuario, MotosDbContext db) =>
    {
        var existingUsuario = await db.Usuarios.FindAsync(id);
        if (existingUsuario == null)
            return Results.NotFound();

        existingUsuario.Email = usuario.Email;
        existingUsuario.Senha = usuario.Senha;
        existingUsuario.Funcao = usuario.Funcao;
    
        await db.SaveChangesAsync();
        return Results.NoContent();
    })
    .WithSummary("Atualiza um usuário existente.");

usuarioGroup.MapDelete("/{id}", async (int id, MotosDbContext db) =>
    {
        var usuario = await db.Usuarios.FindAsync(id);
        if (usuario == null)
            return Results.NotFound();

        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync();
        return Results.NoContent();
    })
    .WithSummary("Deleta um usuário.");

// CRUD para a Entidade Setor
var setorGroup = app.MapGroup("/setores").WithTags("Setores");

setorGroup.MapGet("/", async (MotosDbContext db) =>
        await db.Setores.Include(s => s.Motos).ToListAsync())
    .WithSummary("Retorna todos os setores, incluindo as motos de cada um.");

setorGroup.MapGet("/{id}", async (int id, MotosDbContext db) =>
        await db.Setores.Include(s => s.Motos)
            .FirstOrDefaultAsync(s => s.IdSetor == id) is { } setor
            ? Results.Ok(setor)
            : Results.NotFound())
    .WithSummary("Retorna um setor pelo ID, com as motos associadas.");

setorGroup.MapPost("/", async (Setor setor, MotosDbContext db) =>
    {
        db.Setores.Add(setor);
        await db.SaveChangesAsync();
        return Results.Created($"/setores/{setor.IdSetor}", setor);
    })
    .WithSummary("Cria um novo setor.")
    .AddEndpointFilter<IdempotentAPIEndpointFilter>();

setorGroup.MapPut("/{id}", async (int id, Setor setor, MotosDbContext db) =>
    {
        var existingSetor = await db.Setores.FindAsync(id);
        if (existingSetor == null)
            return Results.NotFound();

        existingSetor.Nome = setor.Nome;
    
        await db.SaveChangesAsync();
        return Results.NoContent();
    })
    .WithSummary("Atualiza um setor existente.");

setorGroup.MapDelete("/{id}", async (int id, MotosDbContext db) =>
    {
        var setor = await db.Setores.FindAsync(id);
        if (setor == null)
            return Results.NotFound();

        db.Setores.Remove(setor);
        await db.SaveChangesAsync();
        return Results.NoContent();
    })
    .WithSummary("Deleta um setor.");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<MotosDbContext>();
    //DataSeeder.SeedData(context);
}

app.Run();