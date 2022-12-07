using Contact.Api.Models;

namespace Contact.Api.Infrastructure.Repositories
{
    public interface IContactInformationRepository
    {
        public Task<IEnumerable<ContactInformation>> GetAllAsync();

        public Task<ContactInformation?> GetByIdAsync(Guid id);

        public Task<IEnumerable<ContactInformation>> GetAllOfContactByContactGuidAsync(Guid contactId);

        public Task<Guid> AddAsync(ContactInformation contactInformation);

        public Task DeleteAsync(ContactInformation contactInformation);
    }
}
