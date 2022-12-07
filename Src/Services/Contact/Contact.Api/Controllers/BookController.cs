using Contact.Api.Daos;
using Contact.Api.Enums;
using Contact.Api.Infrastructure;
using Contact.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Contact.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly BookContext _bookContext;

        public BookController(BookContext dbContext)
        {
            _bookContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        [HttpGet]
        [Route("GetContactById")]
        [ProducesResponseType(typeof(ContactWithContactInformationDao), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<ContactWithContactInformationDao>> GetContactByIdAsync([FromQuery] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("The given GUID is empty.");

            var parseResult = Guid.TryParse(id, out var guid);
            if (!parseResult)
                return BadRequest("The given GUID is not in a valid format.");

            var contact = await _bookContext.Contacts.FirstOrDefaultAsync(contact => contact.Id == guid);
            if (contact == default(Models.Contact))
                return NotFound();

            var contactInformation = await _bookContext.ContactInformation.Where(ci => ci.ContactId == contact.Id).ToArrayAsync();

            var result = new ContactWithContactInformationDao
            {
                Id = contact.Id,
                Name = contact.Name,
                Surname = contact.Surname,
                Company = contact.Company,
                ContactInformation = contactInformation.Select(ci => new ContactInformationWithoutContactIdDao
                {
                    Type = ci.Type,
                    Content = ci.Content
                })
            };

            return Ok(result);
        }

        [HttpPost]
        [Route("CreateContact")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateContactAsync(Models.Contact contact)
        {
            var nameTrimmed = contact.Name.Trim();
            var surnameTrimmed = contact.Surname.Trim();

            if (string.IsNullOrWhiteSpace(nameTrimmed))
                return BadRequest("The given name is empty.");

            if (string.IsNullOrWhiteSpace(surnameTrimmed))
                return BadRequest("The given surname is empty.");

            var newContact = new Models.Contact
            {
                Name = nameTrimmed,
                Surname = surnameTrimmed,
                Company = contact.Company.Trim()
            };

            await _bookContext.AddAsync(newContact);
            await _bookContext.SaveChangesAsync();

            return Ok(newContact.Id);
        }

        [HttpDelete]
        [Route("DeleteContactById")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteContactByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("The given GUID is empty.");

            var parseResult = Guid.TryParse(id, out var guid);
            if (!parseResult)
                return BadRequest("The given GUID is not in a valid format.");

            var contact = await _bookContext.Contacts.FirstOrDefaultAsync(contact => contact.Id == guid);
            if (contact == default(Models.Contact))
                return NotFound();

            _bookContext.Contacts.Remove(contact);
            await _bookContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Route("AddContactInformationToContact")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> AddContactInformationToContactAsync(ContactInformationDao contactInformationDao)
        {
            var contentTrimmed = contactInformationDao.Content.Trim();

            if (string.IsNullOrWhiteSpace(contentTrimmed))
                return BadRequest("The given contact information content is empty.");

            if (!Enum.IsDefined(typeof(ContactInformationType), contactInformationDao.Type))
                return BadRequest("The given contact information type is invalid.");

            var contact = await _bookContext.Contacts.FirstOrDefaultAsync(contact => contact.Id == contactInformationDao.ContactId);
            if (contact == default(Models.Contact))
                return NotFound();

            var newContactInformation = new ContactInformation
            {
                ContactId = contact.Id,
                Type = contactInformationDao.Type,
                Content = contentTrimmed
            };

            var existingContactInformation = await _bookContext.ContactInformation
                .Where(ci => ci.ContactId == newContactInformation.ContactId)
                .ToListAsync();

            if (existingContactInformation.Any(ci => ci.Type == newContactInformation.Type && ci.Content == newContactInformation.Content))
            {
                return BadRequest("This contact information already exists.");
            }

            await _bookContext.AddAsync(newContactInformation);
            await _bookContext.SaveChangesAsync();

            return Ok(newContactInformation.Id);
        }

        [HttpDelete]
        [Route("DeleteContactInformation")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteContactInformationAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("The given GUID is empty.");

            var parseResult = Guid.TryParse(id, out var guid);
            if (!parseResult)
                return BadRequest("The given GUID is not in a valid format.");

            var contactInformation = await _bookContext.ContactInformation.FirstOrDefaultAsync(contact => contact.Id == guid);
            if (contactInformation == default(ContactInformation))
                return NotFound();

            _bookContext.ContactInformation.Remove(contactInformation);
            await _bookContext.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        [Route("GetPhoneBook")]
        [ProducesResponseType(typeof(IEnumerable<ContactWithContactInformationDao>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<ContactWithContactInformationDao>>> GetPhoneBookAsync()
        {
            var contacts = await _bookContext.Contacts.ToArrayAsync();
            var contactInformation = await _bookContext.ContactInformation.ToListAsync();

            var result = new List<ContactWithContactInformationDao>();
            foreach (var contact in contacts)
            {
                result.Add(new ContactWithContactInformationDao
                {
                    Id = contact.Id,
                    Name = contact.Name,
                    Surname = contact.Surname,
                    Company = contact.Company,
                    ContactInformation = contactInformation.Where(ci => ci.ContactId == contact.Id)
                        .Select(ci => new ContactInformationWithoutContactIdDao
                        {
                            Type = ci.Type,
                            Content = ci.Content
                        })
                });

                contactInformation.RemoveAll(ci => ci.Id == contact.Id);
            }

            /*
            This code uses LINQ mapping to create the list, although it doesn't remove already enumerated elements.
            var result = contacts.Select(contact => new ContactWithContactInformationDao
            {
                Id = contact.Id,
                Name = contact.Name,
                Surname = contact.Surname,
                Company = contact.Company,
                ContactInformation = contactInformation.Where(ci => ci.ContactId == contact.Id)
                    .Select(ci => new ContactInformationWithoutContactIdDao
                    {
                        Type = ci.Type,
                        Content = ci.Content
                    })
            });
            */

            return Ok(result);
        }

        [HttpGet]
        [Route("GetAllContactInformation")]
        [ProducesResponseType(typeof(ContactInformationDao), (int)HttpStatusCode.OK)]
        public async Task<ActionResult> GetAllContactInformationAsync()
        {
            var contactInformation = await _bookContext.ContactInformation.ToListAsync();
            var result = contactInformation.Select(ci => new ContactInformationDao
            {
                ContactId = ci.ContactId,
                Type = ci.Type,
                Content = ci.Content
            });

            return Ok(result);
        }
    }
}
