// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using CorrelationId = Cratis.Execution.CorrelationId;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Builds the fixed events every workload appends, so providers are compared on identical data.</summary>
public static class BenchmarkEvents
{
    /// <summary>The event type of the events a projection-like reader selects (every other event).</summary>
    public static readonly EventType Selected = new("benchmark-event", EventTypeGeneration.First);

    /// <summary>The event type of the remaining events.</summary>
    public static readonly EventType Other = new("benchmark-other", EventTypeGeneration.First);

    /// <summary>Gets the event type of the event at a sequence number: even numbers are <see cref="Selected"/>, so a type filter selects half the log.</summary>
    /// <param name="sequenceNumber">The sequence number.</param>
    /// <returns>The event type.</returns>
    public static EventType TypeAt(ulong sequenceNumber) => sequenceNumber % 2 == 0 ? Selected : Other;

    /// <summary>Gets the event source id of the event at a sequence number; one hundred distinct sources are used.</summary>
    /// <param name="sequenceNumber">The sequence number.</param>
    /// <returns>The event source id.</returns>
    public static string SourceAt(ulong sequenceNumber) => $"source-{sequenceNumber % 100}";

    /// <summary>Builds a payload of a given size.</summary>
    /// <param name="payloadBytes">The approximate payload size in bytes.</param>
    /// <returns>The content.</returns>
    public static ExpandoObject Content(int payloadBytes)
    {
        dynamic content = new ExpandoObject();
        content.value = new string('x', Math.Max(1, payloadBytes));
        return content;
    }

    /// <summary>Builds one event for the batch append.</summary>
    /// <param name="sequenceNumber">The sequence number.</param>
    /// <param name="payloadBytes">The payload size.</param>
    /// <returns>The event.</returns>
    public static EventToAppendToStorage ToAppend(ulong sequenceNumber, int payloadBytes) => new(
        sequenceNumber,
        EventSourceType.Default,
        SourceAt(sequenceNumber),
        EventStreamType.All,
        EventStreamId.Default,
        TypeAt(sequenceNumber),
        CorrelationId.NotSet,
        [],
        [],
        [],
        DateTimeOffset.UtcNow,
        Content(payloadBytes),
        "hash");

    /// <summary>Builds the event types storage every provider is given: it knows the benchmark types and nothing else.</summary>
    /// <returns>The storage.</returns>
    public static IEventTypesStorage CreateEventTypes() => new BenchmarkEventTypes();

    /// <summary>Builds the identity storage every provider is given.</summary>
    /// <returns>The storage.</returns>
    public static IIdentityStorage CreateIdentities() => new BenchmarkIdentities();
}
