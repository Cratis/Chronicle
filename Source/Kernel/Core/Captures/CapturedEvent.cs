// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the read model for an event a capture ingested.
/// </summary>
/// <param name="Id">The identity of the event within its capture, which is its sequence number.</param>
/// <param name="Context">The context the event was appended with.</param>
/// <param name="Content">The JSON representation of the event.</param>
/// <remarks>
/// A capture tags everything it ingests with its own name, so what it ingested is a query over the event log
/// narrowed to that tag rather than anything the capture itself records.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.Captures)]
public record CapturedEvent(string Id, Contracts.Events.EventContext Context, string Content)
{
    /// <summary>
    /// Gets the events a capture has ingested, most recent first.
    /// </summary>
    /// <param name="eventStore">The event store the capture belongs to.</param>
    /// <param name="captureName">The name of the capture, which is the tag its events carry.</param>
    /// <param name="namespace">The namespace the events were ingested into.</param>
    /// <param name="maxEvents">How many events to return at most, most recent first.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the event sequence.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> the event content is rendered with.</param>
    /// <returns>A collection of captured events.</returns>
    internal static async Task<IEnumerable<CapturedEvent>> CapturedEvents(
        string eventStore,
        string captureName,
        string @namespace,
        int maxEvents,
        IStorage storage,
        JsonSerializerOptions jsonSerializerOptions)
    {
        var sequence = storage
            .GetEventStore(eventStore)
            .GetNamespace(string.IsNullOrEmpty(@namespace) ? EventStoreNamespaceName.Default : @namespace)
            .GetEventSequence(EventSequenceId.Log);

        using var cursor = await sequence.GetFromSequenceNumber(
            EventSequenceNumber.First,
            null,
            EventStreamType.All,
            EventStreamId.Default,
            [],
            [new Tag(captureName)]);

        var events = new List<AppendedEvent>();
        while (await cursor.MoveNext())
        {
            events.AddRange(cursor.Current);
        }

        return
        [
            .. events
                .OrderByDescending(@event => (ulong)@event.Context.SequenceNumber)
                .Take(maxEvents <= 0 ? 200 : maxEvents)
                .Select(@event => new CapturedEvent(
                    ((ulong)@event.Context.SequenceNumber).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    @event.Context.ToContract(),
                    JsonSerializer.Serialize(@event.Content, jsonSerializerOptions)))
        ];
    }
}
