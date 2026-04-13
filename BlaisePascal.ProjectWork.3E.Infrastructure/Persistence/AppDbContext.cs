using System;
using System.Collections.Generic;
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

                // Value Object: Cittadinanza
                entity.Property(s => s.Cittadinanza)
                    .HasConversion(
                        c => c.Codice,
                        cod => Cittadinanza.Crea(cod))
                    .HasColumnName("CodiceCittadinanza");

                // Value Object: SceltaCompagno (nullable)
                entity.Property(s => s.SceltaCompagno)
                    .HasConversion(
                        sc => sc == null ? null : sc.Testo,
                        t => string.IsNullOrWhiteSpace(t) ? null : SceltaCompagno.Crea(t))
                    .HasColumnName("SceltaCompagno");

                // Value Object: IndirizzoScolastico
                entity.Property(s => s.IndirizzoScolastico)
                    .HasConversion(
                        i => i.Nome,
                        n => IndirizzoScolastico.Crea(n))
                    .HasColumnName("IndirizzoScolastico")
                    .IsRequired();

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

                // Value Object: Sezione
                entity.Property(c => c.Sezione)
                    .HasConversion(
                        s => s.Valore,
                        v => Sezione.Crea(v))
                    .HasColumnName("Sezione")
                    .IsRequired();

                entity.HasIndex("Sezione").IsUnique();

                // Value Object: IndirizzoScolastico
                entity.Property(c => c.Indirizzo)
                    .HasConversion(
                        i => i.Nome,
                        n => IndirizzoScolastico.Crea(n))
                    .HasColumnName("Indirizzo")
                    .IsRequired();

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
