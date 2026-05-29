using Xunit;
using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Logging;
using MediatR;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Infrastructure.Services;
using AcoTestApi.Infrastructure.Services.Channels;
using AcoTestApi.Domain.Enums;
using AcoTestApi.Domain.Entities;
using System.Threading.Tasks;

namespace AcoTestApi.Tests;

public class ThermalPrinterSimulatorTests
{
    private readonly ThermalPrinterSimulator _sut;
    private readonly ILoggingService _loggingService;
    private readonly IPredictionService _predictionService;
    private readonly ILogger<ThermalPrinterSimulator> _logger;
    private readonly IMediator _mediator;

    public ThermalPrinterSimulatorTests()
    {
        _logger = Substitute.For<ILogger<ThermalPrinterSimulator>>();
        _loggingService = Substitute.For<ILoggingService>();
        _predictionService = new PredictionService(); // use concrete for easier calculations
        _mediator = Substitute.For<IMediator>();
        
        var serviceProvider = Substitute.For<IServiceProvider>();
        var channelFactory = Substitute.For<PrinterChannelFactory>(serviceProvider);
        
        var mockUsbChannel = Substitute.For<IPrinterChannel>();
        mockUsbChannel.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        mockUsbChannel.CloseAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        mockUsbChannel.SendDataAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        mockUsbChannel.ConnectionDetails.Returns("Mock USB Port");
        
        channelFactory.GetChannel(PrinterMode.Usb).Returns(mockUsbChannel);
        
        _sut = new ThermalPrinterSimulator(_logger, _loggingService, _predictionService, channelFactory, _mediator);
    }

    [Fact]
    public void InitialState_ShouldBeDisconnected_AndFullPaper()
    {
        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
        _sut.Mode.Should().Be(PrinterMode.None);
        _sut.ActiveError.Should().Be(PrinterErrorState.None);
        _sut.RemainingPaperLengthCm.Should().Be(5000.0);
    }

    [Fact]
    public async Task ConnectAsync_ShouldSucceed_WhenNoErrorActive()
    {
        // Act
        var result = await _sut.ConnectAsync(PrinterMode.Usb);

        // Assert
        result.Should().BeTrue();
        _sut.ConnectionState.Should().Be(ConnectionState.Connected);
        _sut.Mode.Should().Be(PrinterMode.Usb);
    }

    [Fact]
    public async Task ConnectAsync_ShouldFail_WhenCommErrorActive()
    {
        // Arrange
        _sut.SetSimulatedError(PrinterErrorState.COMM_ERROR);

        // Act
        var result = await _sut.ConnectAsync(PrinterMode.Usb);

        // Assert
        result.Should().BeFalse();
        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public async Task PrintAsync_ShouldFail_WhenPaperIsOut()
    {
        // Arrange
        await _sut.ConnectAsync(PrinterMode.Usb);
        _sut.SetSimulatedError(PrinterErrorState.PAPER_OUT);

        var job = new PrintJob
        {
            JobId = "job1",
            Content = "test text",
            ContentType = "text"
        };

        // Act
        var result = await _sut.PrintAsync(job);

        // Assert
        result.Status.Should().Be("Failed");
        result.ErrorCode.Should().Be(PrinterErrorState.PAPER_OUT.ToString());
    }

    [Fact]
    public async Task PrintAsync_ShouldReducePaper_AndSetLastJob_OnSuccess()
    {
        // Arrange
        await _sut.ConnectAsync(PrinterMode.Usb);
        
        var job = new PrintJob
        {
            JobId = "job2",
            Content = "Hello", // text line length = 0.65cm
            ContentType = "text"
        };

        // Act
        var result = await _sut.PrintAsync(job);

        // Assert
        result.Status.Should().Be("Completed");
        _sut.RemainingPaperLengthCm.Should().Be(5000.0 - 0.65);
        _sut.TotalPrintedCount.Should().Be(1);
        _sut.LastJob.Should().Be(job);
    }
}
