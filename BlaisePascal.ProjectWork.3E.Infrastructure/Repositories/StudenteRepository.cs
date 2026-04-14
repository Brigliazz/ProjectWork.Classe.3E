using Microsoft.EntityFrameworkCore;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using BlaisePascal.ProjectWork._3E.Domain.Enums;
using BlaisePascal.ProjectWork._3E.Domain.Repositories;
using BlaisePascal.ProjectWork._3E.Infrastructure.Persistence;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Repositories
{
    public class StudenteRepository : IStudenteRepository
    {
        private readonly AppDbContext _context;

        public StudenteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Studente?> GetByIdAsync(Guid id)
        {
            return await _context.Studenti.FindAsync(id);
        }

        public async Task<List<Studente>> GetNonAssegnatiAsync()
        {
            var studenti = await _context.Studenti
                .Where(s => s.Stato == StatoAssegnazione.NonAssegnato)
                .ToListAsync();
            CaricaPreferenze(studenti);
            return studenti;
        }

        public async Task<List<Studente>> GetAllAsync()
        {
            var studenti = await _context.Studenti.ToListAsync();
            CaricaPreferenze(studenti);
            return studenti;
        }

        public async Task AddAsync(Studente studente)
        {
            await _context.Studenti.AddAsync(studente);
        }

        public Task UpdateAsync(Studente studente)
        {
            _context.Studenti.Update(studente);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private void CaricaPreferenze(List<Studente> studenti)
        {
            if (studenti.Count == 0) return;

            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=studenti.db");
            connection.Open();
            
            foreach (var studente in studenti)
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT NomeStudenteScelto FROM PreferenzeCompagno WHERE CodiceFiscaleStudente = @cf LIMIT 1";
                command.Parameters.AddWithValue("@cf", studente.CodiceFiscale);
                
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        var nomeScelto = reader.GetString(0);
                        if (!string.IsNullOrWhiteSpace(nomeScelto))
                        {
                            studente.ImpostaPreferenza(nomeScelto);
                        }
                    }
                }
            }
        }
    }
}
