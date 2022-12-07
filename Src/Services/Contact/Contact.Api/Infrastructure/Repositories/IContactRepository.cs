namespace Contact.Api.Infrastructure.Repositories
{
    public interface IContactRepository
    {
        public Task<IEnumerable<Models.Contact>> GetAllAsync();

        public Task<Models.Contact?> GetByIdAsync(Guid id);

        public Task<Guid> AddAsync(Models.Contact contact);

        public Task DeleteAsync(Models.Contact contact);
    }
}
