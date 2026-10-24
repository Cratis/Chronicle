// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Samples the durable patterns observer after the measured catch-up window.
/// </summary>
/// <param name="database">The event-store namespace database.</param>
internal sealed class CatchUpShapePatternSamples(IEventStoreNamespaceDatabase database)
{
    /// <summary>
    /// Samples every 30 seconds until the observer reaches the tail and two consecutive states match, or the wait expires.
    /// </summary>
    /// <param name="timeout">The timeout for each database read.</param>
    /// <param name="wait">The maximum time to wait for settlement.</param>
    /// <param name="tail">The seeded tail sequence number.</param>
    /// <param name="expected">The expected handled count, including the marker.</param>
    /// <returns>Every sample and the final settlement verdict and handled shortfall.</returns>
    public async Task<(object[] Samples, bool PatternsSettled, decimal PatternsHandledShortfall)> Read(TimeSpan timeout, TimeSpan wait, ulong tail, long expected)
    {
        using var deadline = new CancellationTokenSource(wait);
        var samples = new List<object>();
        PatternState? previous = null;
        var settled = false;
        try
        {
            while (!deadline.IsCancellationRequested)
            {
                var current = await Sample(timeout, deadline.Token);
                samples.Add(current.Sample);
                settled = current.State.NextEventSequenceNumber > tail && current.State.InFlightPartitions == 0 && current.State == previous;
                previous = current.State;
                if (settled) break;
                await Task.Delay(TimeSpan.FromSeconds(30), deadline.Token);
            }
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            // Expiry is a negative settlement verdict, not a failed catch-up measurement.
        }

        return (samples.ToArray(), settled, expected - (decimal)(previous?.HandledEventCount ?? 0));
    }

    async Task<(object Sample, PatternState State)> Sample(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        var collection = database.GetObserverStateCollection();
        var state = await collection.Find(observer => observer.Id == "$system.patterns")
            .FirstOrDefaultAsync(deadline.Token);
        var values = new PatternState(state?.NextEventSequenceNumber.Value, state?.LastHandledEventSequenceNumber.Value, state?.InFlightPartitions.Count(), state?.HandledEventCount);
        var sample = new
        {
            ObserverId = "$system.patterns",
            Collection = collection.CollectionNamespace.FullName,
            SampledAt = DateTimeOffset.UtcNow,
            Exists = state is not null,
            values.NextEventSequenceNumber,
            values.LastHandledEventSequenceNumber,
            values.InFlightPartitions,
            values.HandledEventCount
        };
        Console.WriteLine(JsonSerializer.Serialize(new { Scenario = "catch-up-patterns-state", Sample = sample }));
        return (sample, values);
    }

    sealed record PatternState(ulong? NextEventSequenceNumber, ulong? LastHandledEventSequenceNumber, int? InFlightPartitions, ulong? HandledEventCount);
}
