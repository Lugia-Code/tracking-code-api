//using System;
using System.Linq;
using System.Collections.Generic;
using tracking_code_api;

public static class DataSeeder
{
    public static void SeedData(MotosDbContext context)
    {
        // Garantindo que o banco de dados seja excluído e recriado em cada execução
        context.Database.EnsureCreated();

        //// Criando usuários
        // var usuarios = new List<Usuario>
        // {
        //     new Usuario { Nome = "João Silva", Email = "joao.silva@motoapi.com", Senha = "senha123", Funcao = "Gerente" },
        //     new Usuario { Nome = "Maria Souza", Email = "maria.souza@motoapi.com", Senha = "senha456", Funcao = "Tecnico" }
        // };

        // foreach (var u in usuarios)
        // {
        //     if (context.Usuarios.Count(x => x.Email == u.Email) == 0)
        //     {
        //         context.Usuarios.Add(u);
        //     }
        // }
        // context.SaveChanges();

        // // Criando setores
        // var setoresList = new List<Setor>
        // {
        //     new Setor { IdSetor = 1, Nome = "Prontas para alugar", Descricao = "Setor para motos disponíveis para locação" },
        //     new Setor { IdSetor = 2, Nome = "Manutenção", Descricao = "Setor para motos com manutenção agendada" },
        //     new Setor { IdSetor = 3, Nome = "Pendenetes", Descricao = "Setor para motos com pendências" },
        //     new Setor { IdSetor = 4, Nome = "Sem placa", Descricao = "Setor para motos que estão sem placa" },
        //     new Setor { IdSetor = 5, Nome = "Reparo simples", Descricao = "Setor para motos com reparos simples" },
        //     new Setor { IdSetor = 6, Nome = "Danos graves", Descricao = "Setor para motos com danos graves" },
        //     new Setor { IdSetor = 7, Nome = "Motor defeituoso", Descricao = "Setor para motos com problemas no motor" },
        //     new Setor { IdSetor = 8, Nome = "Agendadas para manutencão", Descricao = "Setor para motos com manutenção agendada" }
        // };

        // foreach (var s in setoresList)
        // {
        //     if (context.Setores.Count(x => x.Nome == s.Nome) == 0)
        //     {
        //         context.Setores.Add(s);
        //     }
        // }
        // context.SaveChanges();

        // // Criando tags
        // var tags = new List<Tag>
        // {
        //     new Tag { CodigoTag = "tag12345", Status = "ativo", DataVinculo = DateTime.Now },
        //     new Tag { CodigoTag = "tag54321", Status = "inativo", DataVinculo = DateTime.Now.AddDays(-30) }
        // };

        // foreach (var t in tags)
        // {
        //     if (context.Tags.Count(x => x.CodigoTag == t.CodigoTag) == 0)
        //     {
        //         context.Tags.Add(t);
        //     }
        // }
        // context.SaveChanges();

        // // Criando motos
        // var moto1 = new Moto
        // {
        //     Placa = "ABC1D23",
        //     Chassi = "9BW1234567890DEF",
        //     Modelo = "Honda CB 500",
        //     DataCadastro = DateTime.Now,
        //     IdSetor = setoresList.First(s => s.Nome == "Prontas para alugar").IdSetor,
        //     Setor = setoresList.First(s => s.Nome == "Prontas para alugar"),
        //     CodigoTag = tags.First(t => t.CodigoTag == "tag12345").CodigoTag,
        //     Tag = tags.First(t => t.CodigoTag == "tag12345")
        // };

        // var moto2 = new Moto
        // {
        //     Placa = "XYZ9A87",
        //     Chassi = "9BW0987654321GHI",
        //     Modelo = "Yamaha FZ25",
        //     DataCadastro = DateTime.Now,
        //     IdSetor = setoresList.First(s => s.Nome == "Agendadas para manutencão").IdSetor,
        //     Setor = setoresList.First(s => s.Nome == "Agendadas para manutencão"),
        //     CodigoTag = tags.First(t => t.CodigoTag == "tag54321").CodigoTag,
        //     Tag = tags.First(t => t.CodigoTag == "tag54321")
        // };

        // foreach (var m in new List<Moto> { moto1, moto2 })
        // {
        //     if (context.Motos.Count(x => x.Placa == m.Placa) == 0)
        //     {
        //         context.Motos.Add(m);
        //     }
        // }
        
        context.SaveChanges();
    }
}
