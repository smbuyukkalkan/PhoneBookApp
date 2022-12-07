﻿using Reporting.Api.Models;

namespace Reporting.Api.Infrastructure.Repositories;

public interface IReportRepository
{
    Task<IEnumerable<Report>> GetAllReportsAsync();

    Task<Report?> GetReportByIdAsync(Guid id);

    Task<Guid> CreateNewReportAsync();

    Task UpdateReportAsync(Report report);
}