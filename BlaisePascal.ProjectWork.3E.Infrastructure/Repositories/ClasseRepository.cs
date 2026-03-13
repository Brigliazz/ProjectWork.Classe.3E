using Microsoft.EntityFrameworkCore;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima;
using BlaisePascal.ProjectWork._3E.Domain.Repositories;
using BlaisePascal.ProjectWork._3E.Infrastructure.Persistence;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Repositories
{
    public class ClasseRepository : IClasseRepository
    {
        private readonly AppDbContext _context;

        public ClasseRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ClassePrima?> GetByIdAsync(Guid id)
        {
            return await _context.ClassiPrime.FindAsync(id);
        }

        public async Task<List<ClassePrima>> GetAllAsync()
        {
            return await _context.ClassiPrime.ToListAsync();
        }

        public async Task<ClassePrima?> GetBySezioneAsync(string sezione)
        {
            return await _context.ClassiPrime
                .FirstOrDefaultAsync(c => c.Sezione.Valore == sezione);
        }

        public async Task AddAsync(ClassePrima classe)
        {
            await _context.ClassiPrime.AddAsync(classe);
        }

        public Task UpdateAsync(ClassePrima classe)
        {
            _context.ClassiPrime.Update(classe);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
