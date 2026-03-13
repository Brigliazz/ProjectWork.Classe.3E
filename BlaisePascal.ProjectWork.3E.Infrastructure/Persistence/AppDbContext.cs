using Microsoft.EntityFrameworkCore;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima;
using BlaisePascal.ProjectWork._3E.Domain.Entities;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public DbSet<Studente> Studenti { get; set; }
        public DbSet<ClassePrima> ClassiPrime { get; set; }
        public DbSet<ScuolaProvenienza> ScuoleProvenienza { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ─── STUDENTE ───────────────────────────────────────
            modelBuilder.Entity<Studente>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.CodiceFiscale).IsUnique();

                entity.Property(s => s.Nome).IsRequired();
                entity.Property(s => s.Cognome).IsRequired();
                entity.Property(s => s.CodiceFiscale).IsRequired();
                entity.Property(s => s.CodiceScuolaProvenienza).IsRequired();

                // Value Object: ProfiloBES — owned type
                entity.OwnsOne(s => s.ProfiloBES, bes =>
                {
                    bes.Property(b => b.HasDisabilita).HasColumnName("HasDisabilita");
                    bes.Property(b => b.HasDSA).HasColumnName("HasDSA");
                    bes.Property(b => b.HasDisabilitaAssBase).HasColumnName("HasDisabilitaAssBase");
                });

                // Value Object: Cittadinanza — owned type
                entity.OwnsOne(s => s.Cittadinanza, citt =>
                {
                    citt.Property(c => c.Codice).HasColumnName("CodiceCittadinanza");
                });

                // Value Object: SceltaCompagno — owned type (nullable)
                entity.OwnsOne(s => s.SceltaCompagno, sc =>
                {
                    sc.Property(c => c.Testo).HasColumnName("SceltaCompagno");
                });

                // FK verso ClassePrima — SetNull on delete
                entity.HasOne<ClassePrima>()
                    .WithMany()
                    .HasForeignKey(s => s.ClasseId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── CLASSE PRIMA ───────────────────────────────────
            modelBuilder.Entity<ClassePrima>(entity =>
            {
                entity.HasKey(c => c.Id);

                // Value Object: Sezione — owned type
                entity.OwnsOne(c => c.Sezione, sez =>
                {
                    sez.Property(s => s.Valore).HasColumnName("Sezione").IsRequired();
                    sez.HasIndex(s => s.Valore).IsUnique();
                });

                // Value Object: IndirizzoScolastico — owned type
                entity.OwnsOne(c => c.Indirizzo, ind =>
                {
                    ind.Property(i => i.Nome).HasColumnName("Indirizzo").IsRequired();
                });

                // StudentiIds — serializzato come JSON
                entity.Property<List<Guid>>("_studentiIds")
                    .HasColumnName("StudentiIds")
                    .HasField("_studentiIds");
            });

            // ─── SCUOLA PROVENIENZA ─────────────────────────────
            modelBuilder.Entity<ScuolaProvenienza>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.CodiceScuola).IsUnique();

                entity.Property(s => s.CodiceScuola).IsRequired();
                entity.Property(s => s.NomeScuola).IsRequired();
                entity.Property(s => s.ComuneScuola).IsRequired();
            });
        }
    }
}
