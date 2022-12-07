using Contact.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Contact.Api.Infrastructure.Repositories
{
    public class DbContactInformationRepository : IContactInformationRepository
    {
        private readonly BookContext _bookContext;

        public DbContactInformationRepository(BookContext bookContext)
        {
            _bookContext = bookContext ?? throw new ArgumentNullException(nameof(bookContext));
        }

        public async Task<IEnumerable<ContactInformation>> GetAllAsync()
        {
            return await _bookContext.ContactInformation.ToListAsync();
        }

        public async Task<ContactInformation?> GetByIdAsync(Guid id)
        {
            return await _bookContext.ContactInformation.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<ContactInformation>> GetAllByContactGuidAsync(Guid contactId)
        {
            return await _bookContext.ContactInformation.Where(x => x.ContactId == contactId).ToListAsync();
        }

        public async Task<Guid> AddAsync(ContactInformation contactInformation)
        {
            await _bookContext.AddAsync(contactInformation);
            await _bookContext.SaveChangesAsync();

            return contactInformation.Id;
        }

        public async Task DeleteAsync(ContactInformation contactInformation)
        {
            _bookContext.Remove(contactInformation);
            await _bookContext.SaveChangesAsync();
        }
    }
}
