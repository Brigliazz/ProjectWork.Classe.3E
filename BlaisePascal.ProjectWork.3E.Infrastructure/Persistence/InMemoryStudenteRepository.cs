using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using BlaisePascal.ProjectWork._3E.Domain.Enums;
using BlaisePascal.ProjectWork._3E.Domain.Repositories;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Persistence
{
    /// <summary>
    /// Repository in-memory per test — nessun database necessario.
    /// </summary>
    public class InMemoryStudenteRepository : IStudenteRepository
    {
        private readonly List<Studente> _studenti = new();

        public Task<Studente?> GetByIdAsync(Guid id)
            => Task.FromResult(_studenti.FirstOrDefault(s => s.Id == id));

        public Task<List<Studente>> GetNonAssegnatiAsync()
            => Task.FromResult(_studenti.Where(s => s.Stato == StatoAssegnazione.NonAssegnato).ToList());

        public Task<List<Studente>> GetAllAsync()
            => Task.FromResult(_studenti.ToList());

        public Task AddAsync(Studente studente)
        {
            _studenti.Add(studente);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Studente studente)
            => Task.CompletedTask; // in-memory, già aggiornato per riferimento

        public Task SaveChangesAsync()
            => Task.CompletedTask; // nulla da salvare
    }
}
