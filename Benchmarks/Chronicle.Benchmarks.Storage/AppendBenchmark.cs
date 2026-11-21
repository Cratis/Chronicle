// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using CorrelationId = Cratis.Execution.CorrelationId;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Shares the single-event append operation between batch and concurrent append benchmarks.</summary>
public abstract class AppendBenchmark : StorageBenchmark
{
    /// <summary>Appends one event at the given sequence number.</summary>
    /// <param name="storage">The sequence storage.</param>
    /// <param name="sequenceNumber">The sequence number.</param>
    /// <returns>The task.</returns>
    protected static async Task Append(IEventSequenceStorage storage, ulong sequenceNumber)
    {
        var type = BenchmarkEvents.TypeAt(sequenceNumber);
        var content = new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = BenchmarkEvents.Content(Profile.PayloadBytes) };
        var hashes = new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = "hash" };
        var result = await storage.Append(sequenceNumber, EventSourceType.Default, BenchmarkEvents.SourceAt(sequenceNumber), EventStreamType.All, EventStreamId.Default, type, CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, content, hashes);
        if (!result.IsSuccess) throw new InvalidOperationException("Append failed.");
    }
}
