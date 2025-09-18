using Microsoft.EntityFrameworkCore;

namespace tracking_code_api
{
    public class MotosDbContext : DbContext
    {
        public MotosDbContext(DbContextOptions<MotosDbContext> options) : base(options)
        {
        }

        // DbSets para as entidades.
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Setor> Setores { get; set; }
        public DbSet<Moto> Motos { get; set; }
        public DbSet<AuditoriaMovimentacao> AuditoriasMovimentacoes { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Localizacao> Localizacoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            // Mapeando chaves primárias e nomes de tabelas (para clareza)
            modelBuilder.Entity<Usuario>().ToTable("USUARIO");
            modelBuilder.Entity<AuditoriaMovimentacao>().ToTable("AUDITORIA_MOVIMENTACAO");
            modelBuilder.Entity<Setor>().ToTable("SETOR");
            modelBuilder.Entity<Tag>().ToTable("TAG");
            modelBuilder.Entity<Moto>().ToTable("MOTO");
            modelBuilder.Entity<Localizacao>().ToTable("LOCALIZACAO");

            // Mapeando restrições de unicidade
            modelBuilder.Entity<Moto>().HasIndex(m => m.Placa).IsUnique();
            modelBuilder.Entity<Setor>().HasIndex(s => s.Nome).IsUnique();
            //modelBuilder.Entity<Tag>().HasIndex(t => t.CodigoTag).IsUnique();
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();

            // CORREÇÃO: A precisão das colunas X e Y na tabela "localizacao" deve ser NUMBER(12, 8)
            modelBuilder.Entity<Localizacao>()
                .Property(l => l.X)
                .HasPrecision(12, 8);

            modelBuilder.Entity<Localizacao>()
                .Property(l => l.Y)
                .HasPrecision(12, 8);
            
            // Relacionamentos:
            // Moto (Muitos) -> Setor (Um)
            modelBuilder.Entity<Moto>()
                .HasOne(m => m.Setor)
                .WithMany(s => s.Motos)
                .HasForeignKey(m => m.IdSetor)
                .OnDelete(DeleteBehavior.Restrict);

            // Moto (Um) -> Tag (Um)
            modelBuilder.Entity<Moto>()
                .HasOne(m => m.Tag)
                .WithOne(t => t.Moto)
                .HasForeignKey<Moto>(m => m.CodigoTag)
                .HasPrincipalKey<Tag>(t => t.CodigoTag)
                .IsRequired(true);
            
            // Moto (Um) -> AuditoriaMovimentacao (Um)
            modelBuilder.Entity<Moto>()
                .HasOne(m => m.Auditoria)
                .WithMany()
                .HasForeignKey(m => m.IdAudit)
                .IsRequired(false);
            
            // Localizacao (Muitos) -> Setor (Um)
            modelBuilder.Entity<Localizacao>()
                .HasOne(l => l.Setor)
                .WithMany(s => s.Localizacoes)
                .HasForeignKey(l => l.IdSetor)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Localizacao (Muitos) -> Tag (Um)
            modelBuilder.Entity<Localizacao>()
                .HasOne(l => l.Tag)
                .WithMany(t => t.Localizacoes)
                .HasForeignKey(l => l.CodigoTag)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Usuario (Um) -> AuditoriaMovimentacao (Muitos)
            modelBuilder.Entity<AuditoriaMovimentacao>()
                .HasOne(a => a.Usuario)
                .WithMany(u => u.Auditorias)
                .HasForeignKey(a => a.IdFuncionario)
                .OnDelete(DeleteBehavior.Restrict);
            
            //id auto-gerado
            modelBuilder.Entity<AuditoriaMovimentacao>()
                .Property(a => a.IdAudit)
                .ValueGeneratedOnAdd();
        }
    }
}