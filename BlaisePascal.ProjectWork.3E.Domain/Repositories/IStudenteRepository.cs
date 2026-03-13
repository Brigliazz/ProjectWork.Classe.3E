using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;

namespace BlaisePascal.ProjectWork._3E.Domain.Repositories
{
    public interface IStudenteRepository
    {
        Task<Studente?> GetByIdAsync(Guid id);
        Task<List<Studente>> GetNonAssegnatiAsync();
        Task<List<Studente>> GetAllAsync();
        Task AddAsync(Studente studente);
        Task UpdateAsync(Studente studente);
        Task SaveChangesAsync();
    }
}
