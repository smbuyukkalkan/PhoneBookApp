using Microsoft.EntityFrameworkCore;

namespace Contact.Api.Infrastructure.Repositories
{
    public class DbContactRepository : IContactRepository
    {
        private readonly BookContext _bookContext;

        public DbContactRepository(BookContext bookContext)
        {
            _bookContext = bookContext ?? throw new ArgumentNullException(nameof(bookContext));
        }

        public async Task<IEnumerable<Models.Contact>> GetAllAsync()
        {
            return await _bookContext.Contacts.ToListAsync();
        }

        public async Task<Models.Contact?> GetByIdAsync(Guid id)
        {
            return await _bookContext.Contacts.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid> AddAsync(Models.Contact contact)
        {
            await _bookContext.AddAsync(contact);
            await _bookContext.SaveChangesAsync();

            return contact.Id;
        }

        public async Task DeleteAsync(Models.Contact contact)
        {
            _bookContext.Remove(contact);
            await _bookContext.SaveChangesAsync();
        }
    }
}
