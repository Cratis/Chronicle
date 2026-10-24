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
    /// Reads two observer-state samples at least one minute apart.
    /// </summary>
    /// <param name="timeout">The timeout for each database read.</param>
    /// <returns>The two samples, including an explicit indication if the observer document is absent.</returns>
    public async Task<object[]> Read(TimeSpan timeout)
    {
        var first = await Sample(timeout);
        await Task.Delay(TimeSpan.FromSeconds(60));
        var second = await Sample(timeout);
        return [first, second];
    }

    async Task<object> Sample(TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        var collection = database.GetObserverStateCollection();
        var state = await collection.Find(observer => observer.Id == "$system.patterns")
            .FirstOrDefaultAsync(deadline.Token);
        var sample = new
        {
            ObserverId = "$system.patterns",
            Collection = collection.CollectionNamespace.FullName,
            SampledAt = DateTimeOffset.UtcNow,
            Exists = state is not null,
            NextEventSequenceNumber = state?.NextEventSequenceNumber.Value,
            LastHandledEventSequenceNumber = state?.LastHandledEventSequenceNumber.Value,
            InFlightPartitions = state?.InFlightPartitions.Count(),
            state?.HandledEventCount
        };
        Console.WriteLine(JsonSerializer.Serialize(new { Scenario = "catch-up-patterns-state", Sample = sample }));
        return sample;
    }
}
