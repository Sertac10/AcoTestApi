using System.Collections.Generic;
using AcoTestApi.Domain.Entities;

namespace AcoTestApi.Application.Common.Interfaces;

public interface IPredictionService
{
    double EstimateJobLengthCm(string content, string contentType);
    double EstimateJobDurationMs(double estimatedLengthCm);
    double CalculateRemainingRollPercentage(double remainingLengthCm, double totalLengthCm);
    int EstimateRemainingReceipts(double remainingLengthCm, double averageReceiptLengthCm = 15.0);
    double CalculateQueueTotalDurationMs(IEnumerable<PrintJob> pendingJobs);
}
