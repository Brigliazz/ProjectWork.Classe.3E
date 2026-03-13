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
            return await _context.Studenti
                .Where(s => s.Stato == StatoAssegnazione.NonAssegnato)
                .ToListAsync();
        }

        public async Task<List<Studente>> GetAllAsync()
        {
            return await _context.Studenti.ToListAsync();
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
    }
}
