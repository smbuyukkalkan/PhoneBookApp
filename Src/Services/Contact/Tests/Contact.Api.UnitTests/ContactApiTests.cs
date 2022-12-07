using System.Collections;
using Contact.Api.Controllers;
using Contact.Api.Daos;
using Contact.Api.Enums;
using Contact.Api.Infrastructure.Repositories;
using Contact.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using Contact = Contact.Api.Models.Contact;

// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo

namespace Contact.Api.UnitTests;

public class ContactApiTests
{
    private readonly Mock<IContactRepository> _contactRepository;
    private readonly Mock<IContactInformationRepository> _contactInformationRepository;
    private BookController? _bookController;

    public ContactApiTests()
    {
        _contactRepository = new Mock<IContactRepository>();
        _contactInformationRepository = new Mock<IContactInformationRepository>();
    }

    #region GetContactById Tests

    public static IEnumerable<object?[]> InvalidFakeGuids => new List<object?[]>
    {
        new object?[] { null },
        new object[] { string.Empty },
        new object[] { "" },
        new object[] { " \n\t\r" },
        new object[] { "abcdefghijklmnopqrstuvwxyzğüşıöç" },
        new object[] { "01234567_89ab_cdef_0123_456789abcdef" }
    };

    [Theory]
    [MemberData(nameof(InvalidFakeGuids))]
    public async Task GetReportById_Returns_BadRequest_Given_Invalid_Guids(string guid)
    {
        // Arrange
        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.GetContactByIdAsync(guid);
        var resultConverted = result as ObjectResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, resultConverted!.StatusCode);
    }

    [Fact]
    public async Task GetContactById_Returns_Contact_Details_Given_Valid_Id()
    {
        // Arrange
        _contactRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Models.Contact());

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.GetContactByIdAsync(Guid.NewGuid().ToString());
        var resultConverted = result as ObjectResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, resultConverted!.StatusCode);
        Assert.NotNull(resultConverted.Value);
        Assert.IsType<ContactWithContactInformationDao>(resultConverted.Value);
    }

    [Fact]
    public async Task GetContactById_Returns_NotFound_For_Non_Existent_Guids()
    {
        // Arrange
        _contactRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(default(Models.Contact));

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.GetContactByIdAsync(Guid.NewGuid().ToString());
        var resultConverted = result as StatusCodeResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, resultConverted!.StatusCode);
    }

    #endregion

    #region CreateContact Tests

    public static IEnumerable<object[]> ExampleInvalidContacts => new List<object[]>
    {
        new object[]
        {
            new Models.Contact
            {
                Name = "  ",
                Surname = " Surname ",
                Company = "The Technology Company"
            }
        },

        new object[]
        {
            new Models.Contact
            {
                Name = "  Mr. Bean",
                Surname = " \n "
            }
        },

        new object[]
        {
            new Models.Contact
            {
                Company = "    "
            }
        }
    };

    [Theory]
    [MemberData(nameof(ExampleInvalidContacts))]
    public async Task CreateContact_Returns_BadRequest_For_Whitespace_Names_Or_Surnames(Models.Contact contact)
    {
        // Arrange
        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.CreateContactAsync(contact);
        var resultConverted = result as ObjectResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, resultConverted!.StatusCode);
    }

    public static IEnumerable<object[]> ExampleContacts => new List<object[]>
    {
        new object[]
        {
            new Models.Contact
            {
                Name = " James",
                Surname = "Sullivan ",
                Company = " Monsters, Inc."
            }
        },

        new object[]
        {
            new Models.Contact
            {
                Name = " Unemployed     ",
                Surname = "   Man "
            }
        },

        new object[]
        {
            new Models.Contact
            {
                Name = "Gordon",
                Surname = "Freeman",
                Company = "Black Mesa Research Facility"
            }
        },
    };

    [Theory]
    [MemberData(nameof(ExampleContacts))]
    public async Task CreateContact_Returns_Ok_For_Valid_Contact_Data(Models.Contact contact)
    {
        // Arrange
        _contactRepository
            .Setup(x => x.AddAsync(It.IsAny<Models.Contact>()))
            .ReturnsAsync(Guid.NewGuid());

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.CreateContactAsync(contact);
        var resultConverted = result as ObjectResult;

        // Assert
        Assert.NotNull(resultConverted!.Value);
        Assert.IsType<Guid>(resultConverted.Value);
    }

    #endregion

    #region DeleteContactById Tests

    [Theory]
    [MemberData(nameof(InvalidFakeGuids))]
    public async Task DeleteContactById_Returns_BadRequest_Given_Invalid_Guids(string guid)
    {
        // Arrange
        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.DeleteContactByIdAsync(guid);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteContactById_Returns_NotFound_Given_Non_Existent_Contact()
    {
        // Arrange
        _contactRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(default(Models.Contact));

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.DeleteContactByIdAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteContactById_Returns_Ok_Given_Valid_Guids()
    {
        // Arrange
        _contactRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Models.Contact());

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.DeleteContactByIdAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.IsType<OkResult>(result);
    }

    #endregion

    #region AddContactInformationToContact Tests

    public static IEnumerable<object[]> ExampleContactInformationWithWhitespaceContent => new List<object[]>
    {
        new object[]
        {
            new ContactInformationDao
            {
                Content = string.Empty
            }
        },

        new object[]
        {
            new ContactInformationDao
            {
                Content = " \r\n\t  "
            }
        }
    };

    [Theory]
    [MemberData(nameof(ExampleContactInformationWithWhitespaceContent))]
    public async Task AddContactInformationToContact_Returns_BadRequest_Given_Whitespace_Content(ContactInformationDao contactInformationDao)
    {
        // Arrange
        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.AddContactInformationToContactAsync(contactInformationDao);
        var resultConverted = result as ObjectResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, resultConverted!.StatusCode);
    }

    public static IEnumerable<object[]> ExampleContactInformationWithOutOfRangeContactInformationType => new List<object[]>
    {
        new object[]
        {
            new ContactInformationDao
            {
                ContactId = Guid.NewGuid(),
                Content = "example@email.com",
                Type = (ContactInformationType)Random.Shared.Next(Enum.GetValues(typeof(ContactInformationType)).Length + 1, int.MaxValue)
            }
        }
    };

    [Theory]
    [MemberData(nameof(ExampleContactInformationWithOutOfRangeContactInformationType))]
    public async Task AddContactInformationToContact_Returns_BadRequest_Given_Out_Of_Range_Contact_Information_Type(ContactInformationDao contactInformationDao)
    {
        // Arrange
        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.AddContactInformationToContactAsync(contactInformationDao);
        var resultConverted = result as ObjectResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, resultConverted!.StatusCode);
    }

    [Fact]
    public async Task AddContactInformationToContact_Returns_NotFound_Given_Non_Existent_Contact_Id()
    {
        // Arrange
        _contactRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(default(Models.Contact));

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        var exampleContactInformationDao = new ContactInformationDao
        {
            ContactId = Guid.NewGuid(),
            Content = "A",
            Type = ContactInformationType.Location
        };

        // Act
        var result = await _bookController.AddContactInformationToContactAsync(exampleContactInformationDao);
        var resultConverted = result as StatusCodeResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, resultConverted!.StatusCode);
    }

    public static IEnumerable<object[]> ExampleContactInformation => new List<object[]>
    {
        new object[]
        {
            new ContactInformation
            {
                ContactId = Guid.NewGuid(),
                Content = "City",
                Type = ContactInformationType.Location
            }
        },

        new object[]
        {
            new ContactInformation
            {
                ContactId = Guid.NewGuid(),
                Content = "+905556667788",
                Type = ContactInformationType.MobileNumber
            }
        },

        new object[]
        {
            new ContactInformation
            {
                ContactId = Guid.NewGuid(),
                Content = "mrincredible@theincredibles.com",
                Type = ContactInformationType.EmailAddress
            }
        }
    };

    [Theory]
    [MemberData(nameof(ExampleContactInformation))]
    public async Task AddContactInformationToContact_Returns_BadRequest_Given_Already_Existing_Contact_Information(ContactInformation contactInformation)
    {
        // Arrange
        _contactRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Models.Contact { Id = contactInformation.ContactId } );

        _contactInformationRepository
            .Setup(x => x.GetAllByContactGuidAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<ContactInformation> { contactInformation });

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        var exampleContactInformationDao = new ContactInformationDao
        {
            ContactId = contactInformation.ContactId,
            Content = contactInformation.Content,
            Type = contactInformation.Type
        };

        // Act
        var result = await _bookController.AddContactInformationToContactAsync(exampleContactInformationDao);
        var resultConverted = result as ObjectResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, resultConverted!.StatusCode);
    }

    public static IEnumerable<object[]> ExampleContactInformationDao => new List<object[]>
    {
        new object[]
        {
            new ContactInformationDao
            {
                ContactId = Guid.NewGuid(),
                Content = "City",
                Type = ContactInformationType.Location
            }
        },

        new object[]
        {
            new ContactInformationDao
            {
                ContactId = Guid.NewGuid(),
                Content = "+905556667788",
                Type = ContactInformationType.MobileNumber
            }
        },

        new object[]
        {
            new ContactInformationDao
            {
                ContactId = Guid.NewGuid(),
                Content = "mrincredible@theincredibles.com",
                Type = ContactInformationType.EmailAddress
            }
        }
    };

    [Theory]
    [MemberData(nameof(ExampleContactInformationDao))]
    public async Task AddContactInformationToContact_Returns_Ok_Given_Valid_Contact_Information(ContactInformationDao contactInformationDao)
    {
        // Arrange
        _contactRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Models.Contact());

        _contactInformationRepository
            .Setup(x => x.AddAsync(It.IsAny<ContactInformation>()))
            .ReturnsAsync(Guid.NewGuid());

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.AddContactInformationToContactAsync(contactInformationDao);
        var resultConverted = result as ObjectResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, resultConverted!.StatusCode);
    }

    #endregion

    #region DeleteContactInformation Tests

    [Theory]
    [MemberData(nameof(InvalidFakeGuids))]
    public async Task DeleteContactInformation_Returns_BadRequest_Given_Invalid_Guids(string guid)
    {
        // Arrange
        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.DeleteContactInformationAsync(guid);
        var resultConverted = result as ObjectResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, resultConverted!.StatusCode);
    }

    [Fact]
    public async Task DeleteContactInformation_Returns_NotFound_Given_Non_Existent_Guids()
    {
        // Arrange
        _contactInformationRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(default(ContactInformation));

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.DeleteContactInformationAsync(Guid.NewGuid().ToString());
        var resultConverted = result as StatusCodeResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, resultConverted!.StatusCode);
    }

    [Fact]
    public async Task DeleteContactInformation_Returns_Ok_Given_Valid_Guids()
    {
        // Arrange
        _contactInformationRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new ContactInformation());

        _contactInformationRepository
            .Setup(x => x.DeleteAsync(It.IsAny<ContactInformation>()));

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.DeleteContactInformationAsync(Guid.NewGuid().ToString());
        var resultConverted = result as StatusCodeResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, resultConverted!.StatusCode);
    }

    #endregion

    #region GetPhoneBook Tests

    [Fact]
    public async Task GetPhoneBook_Returns_Empty_List_When_No_Contacts_Exist()
    {
        // Arrange
        _contactRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Models.Contact>());

        _contactInformationRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ContactInformation>());

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.GetPhoneBookAsync();
        var resultConverted = result.Result as ObjectResult;

        // Assert
        Assert.Empty((resultConverted!.Value as IEnumerable<ContactWithContactInformationDao>)!);
    }

    public static IEnumerable<object[]> ExamplePhoneBookDictionary => new List<object[]>()
    {
        new object[]
        { 
            new Models.Contact
            {
                Id = Guid.Parse("0123456789abcdef0123456789abcdef"),
                Name = "Mike",
                Surname = "Wazowski",
                Company = "Monsters, Inc."
            },

            new ContactInformation
            {
                Id = Guid.NewGuid(),
                ContactId = Guid.Parse("0123456789abcdef0123456789abcdef"),
                Content = "USA",
                Type = ContactInformationType.Location
            }
        },

        new object[]
        {
            new Models.Contact
            {
                Id = Guid.Parse("fedcba9876543210fedcba9876543210"),
                Name = "Cave",
                Surname = "Johnson",
                Company = "Aperture Laboratories"
            },

            new ContactInformation
            {
                Id = Guid.NewGuid(),
                ContactId = Guid.Parse("fedcba9876543210fedcba9876543210"),
                Content = "cave.johnson@aperturelaboratories.com",
                Type = ContactInformationType.EmailAddress
            }
        }
    };

    [Theory]
    [MemberData(nameof(ExamplePhoneBookDictionary))]
    public async Task GetPhoneBook_Returns_List_Of_Existing_Contacts(Models.Contact contact, ContactInformation contactInformation)
    {
        // Arrange
        _contactRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Models.Contact> { contact });

        _contactInformationRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ContactInformation> { contactInformation });

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.GetPhoneBookAsync();
        var resultConverted = result.Result as ObjectResult;

        // Assert
        Assert.NotEmpty((resultConverted!.Value as IEnumerable<ContactWithContactInformationDao>)!);
    }

    #endregion

    #region GetAllContactInformation Tests

    [Fact]
    public async Task GetAllContactInformation_Returns_Empty_List_When_No_Contact_Information_Exist()
    {
        // Arrange
        _contactInformationRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ContactInformation>());

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.GetAllContactInformationAsync();
        var resultConverted = result as ObjectResult;

        // Assert
        Assert.Empty((resultConverted!.Value as IEnumerable<ContactInformationDao>)!);
    }

    [Theory]
    [MemberData(nameof(ExampleContactInformation))]
    public async Task GetAllContactInformation_Returns_List_Of_Existing_Contact_Information(ContactInformation contactInformation)
    {
        // Arrange
        _contactInformationRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ContactInformation> { contactInformation });

        _bookController = new BookController(_contactRepository.Object, _contactInformationRepository.Object);

        // Act
        var result = await _bookController.GetAllContactInformationAsync();
        var resultConverted = result as ObjectResult;

        // Assert
        Assert.NotEmpty((resultConverted!.Value as IEnumerable<ContactInformationDao>)!);
    }

    #endregion
}