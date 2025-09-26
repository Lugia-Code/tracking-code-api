//motoGroup.MapGet("/", async (MotosDbContext db, int page = 1, int pageSize = 10) =>
// {
//     try
//     {
//         var skip = (page - 1) * pageSize;
//         
//         var motosData = await db.Motos
//             .AsNoTracking()
//             .Skip(skip)
//             .Take(pageSize)
//             .Select(m => new
//             {
//                 Chassi = m.Chassi,
//                 Placa = m.Placa,
//                 Modelo = m.Modelo,
//                 DataCadastro = m.DataCadastro,
//                 IdSetor = m.IdSetor,
//                 CodigoTag = m.CodigoTag
//             })
//             .ToListAsync();
// 
//         // Query separada para setores
//         var setorIds = motosData.Select(m => m.IdSetor).Distinct().ToList();
//         var setoresDict = await db.Setores
//             .AsNoTracking()
//             .Where(s => setorIds.Contains(s.IdSetor))
//             .ToDictionaryAsync(s => s.IdSetor, s => new { s.IdSetor, s.Nome });
// 
//         // Query separada para tags
//         var tagCodes = motosData.Select(m => m.CodigoTag).Distinct().ToList();
//         var tagsDict = await db.Tags
//             .AsNoTracking()
//             .Where(t => tagCodes.Contains(t.CodigoTag))
//             .ToDictionaryAsync(t => t.CodigoTag, t => new { t.CodigoTag, t.Status, t.DataVinculo });
// 
//         // Montar resultado final
//         var result = motosData.Select(m => new
//         {
//             chassi = m.Chassi,
//             placa = m.Placa,
//             modelo = m.Modelo,
//             dataCadastro = m.DataCadastro,
//             setor = setoresDict.ContainsKey(m.IdSetor) 
//                 ? new { idSetor = setoresDict[m.IdSetor].IdSetor, nome = setoresDict[m.IdSetor].Nome }
//                 : null,
//             tag = tagsDict.ContainsKey(m.CodigoTag) 
//                 ? new { 
//                     codigoTag = tagsDict[m.CodigoTag].CodigoTag, 
//                     status = tagsDict[m.CodigoTag].Status, 
//                     dataVinculo = tagsDict[m.CodigoTag].DataVinculo 
//                 }
//                 : null
//         }).ToList();
// 
//         var totalCount = await db.Motos.CountAsync();
//         
//         return Results.Ok(new
//         {
//             data = result,
//             pagination = new
//             {
//                 page,
//                 pageSize,
//                 totalCount,
//                 totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
//             }
//         });
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"Erro: {ex}");
//         return Results.Problem("Erro interno do servidor", statusCode: 500);
//     }
// })
// .WithSummary("Retorna todas as motos com paginação")
// .WithOpenApi();
// 