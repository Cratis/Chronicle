// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Drains a local handler and records its failures best-effort within the shared stop budget.
/// </summary>
/// <param name="grainFactory">The grain factory for recording failed partitions.</param>
/// <param name="logger">The observer service logger.</param>
internal class ObserverBulkDrain(IGrainFactory grainFactory, ILogger<ObserverService> logger)
{
    /// <summary>
    /// Ends a window without allowing a driver or failure-recording delay to extend shutdown.
    /// </summary>
    /// <param name="observer">The observer whose window is open.</param>
    /// <param name="endBulk">The handler's bulk finalization operation.</param>
    /// <param name="cancellationToken">The shared stop deadline.</param>
    /// <returns>The bounded drain operation.</returns>
    internal async Task Drain(ObserverDetails observer, Func<CancellationToken, Task<IEnumerable<Storage.Sinks.FailedPartition>>> endBulk, CancellationToken cancellationToken)
    {
        try
        {
            // Invoke even at an expired deadline, so the sink can cancel an already-running bulk write.
            var drain = endBulk(cancellationToken);
            var failures = drain.IsCompletedSuccessfully
                ? (await drain).ToArray()
                : (await drain.WaitAsync(cancellationToken)).ToArray();
            foreach (var failure in failures)
            {
                logger.DrainPartitionFailed(observer.Key, failure.EventSourceId, failure.EventSequenceNumber, failure.Reason);
            }

            if (failures.Length > 0 && !cancellationToken.IsCancellationRequested)
            {
                await ProjectionBulkFailures.Record(grainFactory, observer, failures).WaitAsync(cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.DrainFailed(exception, observer.Key);
        }
    }
}
