//motoGroup.MapPost("/", async (MotoCreateDto motoDto) =>
//     {
// 
//         // criar scope para obter o DbContext (não fica como argumento do endpoint)
//         using var scope = app.Services.CreateScope();
//         var db = scope.ServiceProvider.GetRequiredService<MotosDbContext>();
// 
//         // Validar se já existe uma moto com o mesmo chassi
//         if (await db.Motos.AnyAsync(m => m.Chassi == motoDto.Chassi))
//         {
//             return Results.BadRequest("Já existe uma moto com este chassi");
//         }
// 
//         // Validar se já existe uma moto com a mesma placa (se fornecida)
//         if (!string.IsNullOrEmpty(motoDto.Placa) &&
//             await db.Motos.AnyAsync(m => m.Placa == motoDto.Placa))
//         {
//             return Results.BadRequest("Já existe uma moto com esta placa");
//         }
// 
//         // Validar se o setor existe
//         if (!await db.Setores.AnyAsync(s => s.IdSetor == motoDto.IdSetor))
//         {
//             return Results.BadRequest("Setor não encontrado");
//         }
// 
//         // Validar se a tag existe
//         var tag = await db.Tags.FindAsync(motoDto.CodigoTag);
//         if (tag == null)
//         {
//             return Results.BadRequest("Tag não encontrada");
//         }
// 
//         // Validar se a tag já está vinculada a outra moto
//         if (await db.Motos.AnyAsync(m => m.CodigoTag == motoDto.CodigoTag))
//         {
//             return Results.BadRequest("Esta tag já está vinculada a outra moto");
//         }
// 
//         var moto = new Moto
//         {
//             Chassi = motoDto.Chassi,
//             Placa = motoDto.Placa,
//             Modelo = motoDto.Modelo,
//             IdSetor = motoDto.IdSetor,
//             CodigoTag = motoDto.CodigoTag,
//             DataCadastro = DateTime.Now
//         };
// 
//         // Atualizar o status da tag para "ativo" quando vinculada à moto
//         tag.VincularMoto(moto.Chassi);
// 
//         db.Motos.Add(moto);
//         await db.SaveChangesAsync();
// 
//         // Carregar dados relacionados para retorno
//         await db.Entry(moto).Reference(m => m.Setor).LoadAsync();
//         await db.Entry(moto).Reference(m => m.Tag).LoadAsync();
// 
//         var resultDto = new MotoReadDto(
//             moto.Chassi,
//             moto.Placa,
//             moto.Modelo,
//             moto.DataCadastro,
//             new SetorReadDto(moto.Setor.IdSetor, moto.Setor.Nome),
//             new TagReadDto(moto.Tag.CodigoTag, moto.Tag.Status, moto.Tag.DataVinculo, moto.Tag.Chassi)
//         );
// 
//         return Results.Created($"/api/v1/motos/{moto.Chassi}", resultDto);
//     })
//     .WithSummary("Cadastra uma nova moto (tag obrigatória)")
//     .WithOpenApi();
// //.AddEndpointFilter<IdempotentAPIEndpointFilter>();




//// Endpoint de POST usando o DTO
// motoGroup.MapPost("/", async (MotoCreateDto motoDto, MotosDbContext db) =>
//     {
//         var moto = new Moto
//         {
//             Placa = motoDto.Placa,
//             Chassi = motoDto.Chassi,
//             Modelo = motoDto.Modelo,
//             IdSetor = string.IsNullOrEmpty(motoDto.Placa) ? 4 : motoDto.IdSetor, // ajuste direto
//             CodigoTag = motoDto.CodigoTag
//         };
// 
//         db.Motos.Add(moto);
//         await db.SaveChangesAsync();
// 
//         // Carrega Setor e Tag para DTO
//         await db.Entry(moto).Reference(m => m.Setor).LoadAsync();
//         await db.Entry(moto).Reference(m => m.Tag).LoadAsync();
// 
//         var resultDto = new MotoReadDto(
//             moto.Chassi,
//             moto.Placa ?? string.Empty,
//             moto.Modelo,
//             moto.DataCadastro,
//             moto.Setor != null ? new SetorReadDto(moto.Setor.IdSetor, moto.Setor.Nome) : null,
//             moto.Tag != null ? new TagReadDto(moto.Tag.CodigoTag, moto.Tag.Status) : null
//         );
// 
//         return TypedResults.Created($"/motos/{moto.Chassi}", resultDto);
//     })
//     .WithSummary("Cadastra uma nova moto.");