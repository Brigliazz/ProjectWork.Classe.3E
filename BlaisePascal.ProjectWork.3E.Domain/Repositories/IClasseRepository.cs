using BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima;

namespace BlaisePascal.ProjectWork._3E.Domain.Repositories
{
    public interface IClasseRepository
    {
        Task<ClassePrima?> GetByIdAsync(Guid id);
        Task<List<ClassePrima>> GetAllAsync();
        Task<ClassePrima?> GetBySezioneAsync(string sezione);
        Task AddAsync(ClassePrima classe);
        Task UpdateAsync(ClassePrima classe);
        Task SaveChangesAsync();
    }
}
