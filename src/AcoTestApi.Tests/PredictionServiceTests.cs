using Xunit;
using FluentAssertions;
using AcoTestApi.Infrastructure.Services;
using AcoTestApi.Domain.Entities;
using System.Collections.Generic;

namespace AcoTestApi.Tests;

public class PredictionServiceTests
{
    private readonly PredictionService _sut; // System Under Test

    public PredictionServiceTests()
    {
        _sut = new PredictionService();
    }

    [Fact]
    public void EstimateJobLengthCm_ShouldReturnStaticLength_ForQrAndImage()
    {
        // Act
        var qrLength = _sut.EstimateJobLengthCm("https://acorecycling.com", "qr");
        var imageLength = _sut.EstimateJobLengthCm("BASE64_IMAGE", "image");

        // Assert
        qrLength.Should().Be(1.5);
        imageLength.Should().Be(2.5);
    }

    [Theory]
    [InlineData("Hello", 0.65)] // 1 line * 0.15 + 0.5
    [InlineData("Line1\nLine2", 0.8)] // 2 lines * 0.15 + 0.5
    public void EstimateJobLengthCm_ShouldCalculateLineBasedLength_ForText(string content, double expectedLength)
    {
        // Act
        var length = _sut.EstimateJobLengthCm(content, "text");

        // Assert
        length.Should().Be(expectedLength);
    }

    [Fact]
    public void CalculateRemainingRollPercentage_ShouldReturnCorrectPercentage()
    {
        // Act
        var pct = _sut.CalculateRemainingRollPercentage(2500.0, 5000.0);

        // Assert
        pct.Should().Be(50.0);
    }

    [Fact]
    public void EstimateRemainingReceipts_ShouldReturnCorrectEstimate()
    {
        // Act
        var receiptsLeft = _sut.EstimateRemainingReceipts(1500.0, 15.0);

        // Assert
        receiptsLeft.Should().Be(100);
    }

    [Fact]
    public void CalculateQueueTotalDurationMs_ShouldSumAllDurations()
    {
        // Arrange
        var jobs = new List<PrintJob>
        {
            new() { EstimatedLengthCm = 1.0, ContentType = "qr" }, // 1s
            new() { EstimatedLengthCm = 2.0, ContentType = "image" } // 2s
        };

        // Act
        var totalMs = _sut.CalculateQueueTotalDurationMs(jobs);

        // Assert
        totalMs.Should().Be(3000.0); // 3 seconds total
    }
}
