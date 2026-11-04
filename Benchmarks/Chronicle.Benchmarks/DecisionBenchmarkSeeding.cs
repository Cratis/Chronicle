// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Seeds and validates event histories for the decision read and guarded append benchmarks.
/// </summary>
internal static class DecisionBenchmarkSeeding
{
    const int BatchSize = 1000;
    static readonly TimeSpan _completionTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Appends <paramref name="count"/> events for one key in batches and waits for observers to complete.
    /// </summary>
    /// <param name="log">The <see cref="IEventLog"/> to append to.</param>
    /// <param name="key">The event source to seed.</param>
    /// <param name="count">The number of events to append.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidOperationException">A batch append or its observer completion failed.</exception>
    internal static async Task Seed(IEventLog log, Guid key, int count)
    {
        for (var offset = 0; offset < count; offset += BatchSize)
        {
            var events = Enumerable.Range(offset, Math.Min(BatchSize, count - offset))
                .Select(_ => new BenchmarkInstanceRecorded($"Benchmark{_}", _, DateTimeOffset.UtcNow))
                .Cast<object>()
                .ToArray();
            var result = await log.AppendMany(key, events);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException("The seed batch append failed.");
            }

            var completion = await result.WaitForCompletion(_completionTimeout);
            if (!completion.IsSuccess)
            {
                throw new InvalidOperationException($"Seeding failed for {completion.FailedPartitions.Count()} partition(s).");
            }
        }
    }

    /// <summary>
    /// Verifies that the materialized read and the detached decision read both resolve the expected value.
    /// </summary>
    /// <param name="store">The <see cref="IEventStore"/> to read from.</param>
    /// <param name="reads">The <see cref="IDecisionReads"/> to read with.</param>
    /// <param name="key">The read model key.</param>
    /// <param name="expectedValue">The value both reads must resolve.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidOperationException">Either read resolved a missing or unexpected value.</exception>
    internal static async Task Validate(IEventStore store, IDecisionReads reads, Guid key, int expectedValue)
    {
        var materialized = await store.ReadModels.GetInstanceById<BenchmarkMaterializedReadModel>(key);
        if (materialized is null || materialized.Value != expectedValue)
        {
            throw new InvalidOperationException($"The materialized read resolved value {materialized?.Value}, expected {expectedValue} for {key}.");
        }

        var decision = await reads.GetDetached<BenchmarkMaterializedReadModel>(key);
        if (!decision.Exists || decision.Instance?.Value != expectedValue)
        {
            throw new InvalidOperationException($"The decision read resolved value {decision.Instance?.Value}, expected {expectedValue} for {key}.");
        }
    }
}
