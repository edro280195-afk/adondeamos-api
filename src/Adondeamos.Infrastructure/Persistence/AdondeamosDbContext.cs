using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Exceptions;
using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Adondeamos.Infrastructure.Persistence;

/// <summary>
/// DbContext mapeado al esquema EXISTENTE (db/001_init_schema.sql). No se usan migraciones de EF:
/// el esquema lo manejan los archivos SQL. Aquí solo describimos cómo calzan entidades y columnas.
/// El mapeo de nombres a snake_case lo aplica UseSnakeCaseNamingConvention en la configuración del contexto.
/// Implementa <see cref="IUnitOfWork"/>: el propio contexto confirma los cambios (SaveChangesAsync).
/// </summary>
public class AdondeamosDbContext : DbContext, IUnitOfWork
{
    public AdondeamosDbContext(DbContextOptions<AdondeamosDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Save> Saves => Set<Save>();
    public DbSet<List> Lists => Set<List>();
    public DbSet<ListItem> ListItems => Set<ListItem>();
    public DbSet<DecisionSession> DecisionSessions => Set<DecisionSession>();
    public DbSet<DecisionOption> DecisionOptions => Set<DecisionOption>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<DecisionMatch> DecisionMatches => Set<DecisionMatch>();

    /// <summary>
    /// Traduce las violaciones de unicidad de PostgreSQL (índices únicos del esquema) a
    /// <see cref="ConflictException"/> para que la API responda 409 en lugar de 500.
    /// </summary>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("La operación viola una restricción de unicidad (registro duplicado).");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Enums de PostgreSQL. El nombre del tipo casa con el esquema; las etiquetas se
        //     traducen desde los nombres de los miembros (snake_case) en la configuración del data source.
        modelBuilder.HasPostgresEnum<PlaceOrigin>(name: "place_origin");
        modelBuilder.HasPostgresEnum<SocialNetwork>(name: "social_network");
        modelBuilder.HasPostgresEnum<SaveStatus>(name: "save_status");
        modelBuilder.HasPostgresEnum<ContentVisibility>(name: "content_visibility");
        modelBuilder.HasPostgresEnum<GroupRole>(name: "group_role");

        // --- users
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(u => u.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
        });

        // --- groups (sin updated_at en el esquema)
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(g => g.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        });

        // --- group_members (PK compuesta)
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(gm => new { gm.GroupId, gm.UserId });
            entity.Property(gm => gm.JoinedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.HasOne(gm => gm.Group)
                  .WithMany(g => g.Members)
                  .HasForeignKey(gm => gm.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(gm => gm.User)
                  .WithMany()
                  .HasForeignKey(gm => gm.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- places
        modelBuilder.Entity<Place>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(p => p.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            entity.Property(p => p.Latitude).HasColumnType("numeric(9,6)");
            entity.Property(p => p.Longitude).HasColumnType("numeric(9,6)");
        });

        // --- saves
        modelBuilder.Entity<Save>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(s => s.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(s => s.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            entity.HasOne(s => s.Place)
                  .WithMany()
                  .HasForeignKey(s => s.PlaceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- lists
        modelBuilder.Entity<List>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(l => l.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(l => l.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
        });

        // --- list_items (PK compuesta)
        modelBuilder.Entity<ListItem>(entity =>
        {
            entity.HasKey(li => new { li.ListId, li.SaveId });
            entity.Property(li => li.AddedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.HasOne(li => li.List)
                  .WithMany(l => l.Items)
                  .HasForeignKey(li => li.ListId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(li => li.Save)
                  .WithMany()
                  .HasForeignKey(li => li.SaveId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- decision_sessions
        modelBuilder.Entity<DecisionSession>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(d => d.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        });

        // --- decision_options
        modelBuilder.Entity<DecisionOption>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.HasOne(o => o.Session)
                  .WithMany(s => s.Options)
                  .HasForeignKey(o => o.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(o => o.Place)
                  .WithMany()
                  .HasForeignKey(o => o.PlaceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- votes
        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(v => v.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.HasOne(v => v.Option)
                  .WithMany(o => o.Votes)
                  .HasForeignKey(v => v.OptionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- decision_matches (PK compuesta, sin clave subrogada)
        modelBuilder.Entity<DecisionMatch>(entity =>
        {
            entity.HasKey(m => new { m.SessionId, m.PlaceId });
            entity.Property(m => m.MatchedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        });
    }
}
