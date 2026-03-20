using BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima;
using BlaisePascal.ProjectWork._3E.Domain.Repositories;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Persistence
{
    /// <summary>
    /// Repository in-memory per test — nessun database necessario.
    /// </summary>
    public class InMemoryClasseRepository : IClasseRepository
    {
        private readonly List<ClassePrima> _classi = new();

        public Task<ClassePrima?> GetByIdAsync(Guid id)
            => Task.FromResult(_classi.FirstOrDefault(c => c.Id == id));

        public Task<List<ClassePrima>> GetAllAsync()
            => Task.FromResult(_classi.ToList());

        public Task<ClassePrima?> GetBySezioneAsync(string sezione)
            => Task.FromResult(_classi.FirstOrDefault(c => c.Sezione.Valore == sezione));

        public Task AddAsync(ClassePrima classe)
        {
            _classi.Add(classe);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ClassePrima classe)
            => Task.CompletedTask;

        public Task SaveChangesAsync()
            => Task.CompletedTask;
    }
}
