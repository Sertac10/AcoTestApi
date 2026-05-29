using Xunit;
using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Logging;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Infrastructure.Services;
using AcoTestApi.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AcoTestApi.Tests;

public class PrintQueueServiceTests
{
    private readonly PrintQueueService _sut;
    private readonly IThermalPrinter _printer;
    private readonly IPredictionService _predictionService;
    private readonly ILogger<PrintQueueService> _logger;

    public PrintQueueServiceTests()
    {
        _printer = Substitute.For<IThermalPrinter>();
        _predictionService = Substitute.For<IPredictionService>();
        _logger = Substitute.For<ILogger<PrintQueueService>>();

        _sut = new PrintQueueService(_printer, _predictionService, _logger);
    }

    [Fact]
    public async Task EnqueueJobAsync_ShouldAddJobToQueue()
    {
        // Arrange
        _predictionService.EstimateJobLengthCm(Arg.Any<string>(), Arg.Any<string>()).Returns(1.5);

        // Act
        var job = await _sut.EnqueueJobAsync("test text", "text");

        // Assert
        job.Should().NotBeNull();
        job.Status.Should().Be("Pending");
        _sut.GetQueueSummary().Should().ContainSingle(j => j.JobId == job.JobId);
    }

    [Fact]
    public async Task GetFailedJobs_ShouldReturnFailedJobs()
    {
        // Arrange
        _predictionService.EstimateJobLengthCm(Arg.Any<string>(), Arg.Any<string>()).Returns(1.5);
        var job = await _sut.EnqueueJobAsync("test text", "text");
        job.Status = "Failed";

        // Act
        var failedJobs = _sut.GetFailedJobs();

        // Assert
        failedJobs.Should().ContainSingle(j => j.JobId == job.JobId);
    }
}
