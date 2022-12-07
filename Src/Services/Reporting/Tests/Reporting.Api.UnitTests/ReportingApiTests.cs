using Microsoft.AspNetCore.Mvc;
using Reporting.Api.Controllers;
using Reporting.Api.Enums;
using Reporting.Api.Infrastructure.Repositories;
using Reporting.Api.IntegrationEvents;
using Reporting.Api.IntegrationEvents.Events;
using Reporting.Api.Models;
using System.Globalization;

// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo

namespace Reporting.Api.UnitTests;

public class ReportingApiTests
{
    private readonly Mock<IReportRepository> _reportRepository;
    private readonly Mock<IReportingIntegrationEventService> _reportingIntegrationEventService;
    private ReportController? _reportController;

    public ReportingApiTests()
    {
        _reportRepository = new Mock<IReportRepository>();
        _reportingIntegrationEventService = new Mock<IReportingIntegrationEventService>();
    }

    #region GetAllReports Tests

    [Fact]
    public async Task GetAllReports_Returns_Empty_List_When_No_Reports_Exist()
    {
        // Arrange
        _reportRepository
            .Setup(x => x.GetAllReportsAsync())
            .ReturnsAsync(new List<Report>());

        _reportController = new ReportController(_reportRepository.Object, _reportingIntegrationEventService.Object);

        // Act
        var result = await _reportController.GetAllReportsAsync();
        var resultConverted = result.Result as OkObjectResult;

        // Assert
        Assert.Equal((int)System.Net.HttpStatusCode.OK, resultConverted!.StatusCode);
        Assert.Empty((resultConverted.Value as IEnumerable<Report>)!);
    }

    public static IEnumerable<object[]> ExampleReportLists => new List<object[]>
    {
        new object[]
        { 
            new List<Report>
            {
                new()
                {
                    Id = new Guid()
                },

                new()
                {
                    Id = new Guid(),
                    PathToReportFile = @"C:\Reports\0123456789abcdef0123456789abcdef.txt",
                    RequestDate = DateTime.Now - TimeSpan.FromSeconds(3),
                    Status = ReportStatus.Ready
                },

                new()
                {
                    Id = new Guid(),
                    RequestDate = DateTime.Now - TimeSpan.FromSeconds(28),
                    Status = ReportStatus.Failed
                }
            }
        },

        new object[]
        {
            new List<Report>
            {
                new()
                {
                    Id = new Guid(),
                    PathToReportFile = @"/etc/phonebook/cdn/reports/fedcba98765432100123456789abcdef.txt",
                    RequestDate = DateTime.ParseExact("20220327_1456", "yyyyMMdd_HHmm", CultureInfo.InvariantCulture),
                    Status = ReportStatus.Ready
                }
            }
        }
    };

    [Theory]
    [MemberData(nameof(ExampleReportLists))]
    public async Task GetAllReports_Returns_List_Of_Existing_Reports_When_Reports_Exist(List<Report> exampleReportList)
    {
        // Arrange
        _reportRepository
            .Setup(x => x.GetAllReportsAsync())
            .ReturnsAsync(exampleReportList);

        _reportController = new ReportController(_reportRepository.Object, _reportingIntegrationEventService.Object);

        // Act
        var result = await _reportController.GetAllReportsAsync();
        var resultConverted = result.Result as OkObjectResult;

        // Assert
        Assert.Equal((int)System.Net.HttpStatusCode.OK, resultConverted!.StatusCode);
        Assert.NotEmpty((resultConverted.Value as IEnumerable<Report>)!);
    }

    #endregion

    #region GetReportByIdAsync Tests
         
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
        _reportController = new ReportController(_reportRepository.Object, _reportingIntegrationEventService.Object);

        // Act
        var result = await _reportController.GetReportByIdAsync(guid);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetReportById_Returns_NotFound()
    {
        // Arrange
        _reportRepository
            .Setup(x => x.GetReportByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(default(Report));

        _reportController = new ReportController(_reportRepository.Object, _reportingIntegrationEventService.Object);

        // Act
        var result = await _reportController.GetReportByIdAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    public static IEnumerable<object[]> ExampleReports => new List<object[]>
    {
        new object[]
        {
            new Report
            {
                Id = new Guid()
            }
        },

        new object[]
        {
            new Report
            {
                Id = new Guid(),
                PathToReportFile = "C:/Some/Nonimportant/Path/To/A/File.txt",
                RequestDate = DateTime.Now - TimeSpan.FromSeconds(15),
                Status = ReportStatus.Ready
            }
        },

        new object[]
        {
            new Report
            {
                Id = new Guid(),
                RequestDate = DateTime.Now - TimeSpan.FromSeconds(4),
                Status = ReportStatus.Failed
            }
        }
    };

    [Theory]
    [MemberData(nameof(ExampleReports))]
    public async Task GetReportById_Returns_Report_Details_Given_Valid_Id(Report exampleReportToReturn)
    {
        // Arrange
        _reportRepository
            .Setup(x => x.GetReportByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(exampleReportToReturn);

        _reportController = new ReportController(_reportRepository.Object, _reportingIntegrationEventService.Object);

        // Act
        var result = await _reportController.GetReportByIdAsync(Guid.NewGuid().ToString());
        var resultConverted = result.Result as OkObjectResult;

        // Assert
        Assert.NotNull(resultConverted!.Value);
        Assert.IsType<Report>(resultConverted.Value);
    }

    #endregion

    #region GenerateReport Tests

    [Fact]
    public async Task GenerateReport_Returns_Guid_For_Successful_Report_Generation_Request()
    {
        // Arrange
        _reportRepository
            .Setup(x => x.CreateNewReportAsync())
            .ReturnsAsync(new Guid());

        _reportingIntegrationEventService
            .Setup(x => x.PublishThroughEventBus(It.IsAny<ReportRequestedIntegrationEvent>()));

        _reportController = new ReportController(_reportRepository.Object, _reportingIntegrationEventService.Object);

        // Act
        var result = await _reportController.GenerateReportAsync();
        var resultConverted = result.Result as OkObjectResult;

        // Assert
        Assert.IsType<Guid>(resultConverted!.Value);
    }

    #endregion
}