using Microsoft.EntityFrameworkCore;
using QualityDoc.API.Models;

namespace QualityDoc.API.Data
{
    public class QualityDocDbContext : DbContext
    {
        // El constructor recibe las opciones (como la cadena de conexión) desde el Program.cs
        public QualityDocDbContext(DbContextOptions<QualityDocDbContext> options) : base(options)
        {
        }

        // =======================================================
        // 1. REGISTRO DE TABLAS (DbSets)
        // =======================================================
        public DbSet<Role> Roles { get; set; }
        public DbSet<Norm> Norms { get; set; }
        public DbSet<DocumentStatus> DocumentStatuses { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<DocumentCategory> DocumentCategories { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        
        // 🚀 NUEVA TABLA: La plantilla de los pasos del workflow
        //public DbSet<DocumentSignatureStep> DocumentSignatureSteps { get; set; } 
        
        public DbSet<DocumentApproval> DocumentApprovals { get; set; }

        // =======================================================
        // 2. CONFIGURACIÓN FLUÍDA (Fluent API)
        // =======================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // A. RESTRICCIONES ÚNICAS (Mapeo de los UNIQUE Constraints de SQL)
            modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
            modelBuilder.Entity<Norm>().HasIndex(n => n.NormName).IsUnique();
            modelBuilder.Entity<DocumentStatus>().HasIndex(ds => ds.StatusName).IsUnique();
            modelBuilder.Entity<Company>().HasIndex(c => c.TaxId).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            
            // Llave única compuesta para evitar códigos de documento duplicados en una misma empresa
            modelBuilder.Entity<Document>().HasIndex(d => new { d.CompanyId, d.DocCode }).IsUnique();

            // 🚀 NUEVA RESTRICCIÓN: Un documento no puede tener dos pasos de firma con el mismo orden
            //modelBuilder.Entity<DocumentSignatureStep>().HasIndex(s => new { s.DocId, s.StepOrder }).IsUnique();

            // B. FILTROS GLOBALES DE CONSULTA (Soft Delete)
            // Esto oculta automáticamente la "basura" en cualquier consulta LINQ que hagas después.
            modelBuilder.Entity<Company>().HasQueryFilter(x => x.Status != "Deleted" && x.Status != "Inactive");
            modelBuilder.Entity<Department>().HasQueryFilter(x => x.Status != "Deleted" && x.Status != "Inactive");
            modelBuilder.Entity<User>().HasQueryFilter(x => x.Status != "Deleted" && x.Status != "Inactive");
            modelBuilder.Entity<DocumentCategory>().HasQueryFilter(x => x.Status != "Deleted" && x.Status != "Inactive");
            modelBuilder.Entity<Document>().HasQueryFilter(x => x.Status != "Deleted" && x.Status != "Inactive");
            modelBuilder.Entity<DocumentVersion>().HasQueryFilter(x => x.Status != "Deleted" && x.Status != "Inactive");
            
            // 🚀 NUEVO FILTRO: Aplicado a la nueva tabla de pasos del workflow
            //modelBuilder.Entity<DocumentSignatureStep>().HasQueryFilter(x => x.Status != "Deleted" && x.Status != "Inactive"); 
            
            modelBuilder.Entity<DocumentApproval>().HasQueryFilter(x => x.Status != "Deleted" && x.Status != "Inactive");

            // C. CONFIGURACIÓN DINÁMICA DE VALORES POR DEFECTO Y AUDITORÍA
            // Iteramos sobre todas las tablas de tu modelo para configurar la clase BaseEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Si la tabla hereda de BaseEntity...
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    // 1. Configuración de Fechas y Estados
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("CreatedAt")
                        .HasDefaultValueSql("GETUTCDATE()")
                        .ValueGeneratedOnAdd();

                    modelBuilder.Entity(entityType.ClrType)
                        .Property("Status")
                        .HasDefaultValue("Active");

                    // 2. 🛡️ MAPEO DINÁMICO DE AUDITORÍA (Evita el Delete Cascade en todas las tablas)
                    modelBuilder.Entity(entityType.ClrType)
                        .HasOne("CreatedByNavigation")
                        .WithMany()
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    modelBuilder.Entity(entityType.ClrType)
                        .HasOne("UpdatedByNavigation")
                        .WithMany()
                        .HasForeignKey("UpdatedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    modelBuilder.Entity(entityType.ClrType)
                        .HasOne("DeletedByNavigation")
                        .WithMany()
                        .HasForeignKey("DeletedBy")
                        .OnDelete(DeleteBehavior.Restrict);
                }
            }

            // Valor por defecto específico para la tabla Documents
            modelBuilder.Entity<Document>()
                .Property(d => d.IsExternal)
                .HasDefaultValue(false);


            // =======================================================
            // D. CONFIGURACIÓN DE TRIGGERS (Armadura para el OUTPUT de EF Core)
            // =======================================================
            
            // 🛡️ Agregamos la armadura para la tabla Users:
            modelBuilder.Entity<User>()
                .ToTable(tb => tb.HasTrigger("trg_Users_UpdateTimestamp"));

            modelBuilder.Entity<DocumentVersion>()
                .ToTable(tb => tb.HasTrigger("trg_HandleDocumentObsolescence"));
                
            modelBuilder.Entity<Document>()
                .ToTable(tb => tb.HasTrigger("trg_UpdateDocumentTimestamp"));
        }
    }
}