// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Validates persisted append metadata before exposing any successful local notification.
/// </summary>
/// <remarks>
/// A receipt is the same <see cref="Contracts.Sequences.EventContext"/> a read of the event returns, so an append
/// and a read describe an event identically rather than through two shapes that have to be kept in step.
/// </remarks>
internal static class AppendReceipts
{
    /// <summary>
    /// Validates a single receipt and converts its persisted context.
    /// </summary>
    /// <param name="receipt">The kernel receipt.</param>
    /// <param name="sequenceNumber">The acknowledged sequence number.</param>
    /// <param name="source">The requested source.</param>
    /// <param name="eventType">The requested event type.</param>
    /// <param name="eventStore">The requested event store.</param>
    /// <param name="namespace">The requested namespace.</param>
    /// <returns>The persisted context.</returns>
    /// <exception cref="InvalidAppendReceipt">The receipt is missing or inconsistent.</exception>
    internal static EventContext Convert(
        Contracts.Sequences.EventContext? receipt,
        EventSequenceNumber sequenceNumber,
        EventSourceId source,
        EventType eventType,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace)
    {
        if (receipt is null)
        {
            throw new InvalidAppendReceipt("missing receipt");
        }

        if (sequenceNumber.IsUnavailable || receipt.SequenceNumber != sequenceNumber.Value ||
            receipt.EventSourceId != source.Value || receipt.EventType?.Id != eventType.Id.Value ||
            receipt.EventType.Generation != eventType.Generation.Value || receipt.EventType.Tombstone != eventType.Tombstone)
        {
            throw new InvalidAppendReceipt("sequence, source, or event type does not match the acknowledged input");
        }

        if (receipt.EventStore != eventStore.Value || receipt.Namespace != @namespace.Value)
        {
            throw new InvalidAppendReceipt("event store or namespace does not match the requested destination");
        }

        // Empty values can be valid concept sentinels, including EventHash.NotSet.
        if (receipt.EventSourceType is null || receipt.EventStreamType is null ||
            receipt.EventStreamId is null || receipt.Subject is null || receipt.Hash is null ||
            receipt.Tags?.Any(_ => _ is null) != false || receipt.Causation is null || receipt.CausedBy is null ||
            !DateTimeOffset.TryParse(receipt.Occurred?.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
        {
            throw new InvalidAppendReceipt("persisted context is incomplete or malformed");
        }

        foreach (var causation in receipt.Causation)
        {
            if (causation is null || causation.Type is null ||
                causation.Properties?.Any(_ => _.Value is null) != false ||
                !DateTimeOffset.TryParse(causation.Occurred?.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            {
                throw new InvalidAppendReceipt("persisted causation is incomplete or malformed");
            }
        }

        if (receipt.CausedBy.Subject is null || receipt.CausedBy.Name is null || receipt.CausedBy.UserName is null)
        {
            throw new InvalidAppendReceipt("persisted identity is incomplete or malformed");
        }

        return receipt.ToClient(eventStore, @namespace);
    }

    /// <summary>
    /// Validates the entire ordered batch before returning any persisted contexts.
    /// </summary>
    /// <param name="receipts">The kernel receipts.</param>
    /// <param name="sequenceNumbers">The acknowledged sequence numbers.</param>
    /// <param name="events">The ordered input events.</param>
    /// <param name="eventTypes">The event type registry.</param>
    /// <param name="eventStore">The requested event store.</param>
    /// <param name="namespace">The requested namespace.</param>
    /// <returns>The persisted contexts in input order.</returns>
    /// <exception cref="InvalidAppendReceipt">The receipts are missing or inconsistent.</exception>
    internal static IReadOnlyList<EventContext> ConvertMany(
        IEnumerable<Contracts.Sequences.EventContext> receipts,
        IEnumerable<EventSequenceNumber> sequenceNumbers,
        IReadOnlyList<EventForEventSourceId> events,
        IEventTypes eventTypes,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace)
    {
        var received = receipts?.ToArray() ?? throw new InvalidAppendReceipt("missing batch receipts");
        var numbers = sequenceNumbers.ToArray();
        if (received.Length != events.Count || numbers.Length != events.Count)
        {
            throw new InvalidAppendReceipt("receipt and sequence counts must equal the input count");
        }

        var contexts = new List<EventContext>(events.Count);
        for (var index = 0; index < events.Count; index++)
        {
            if (index > 0 && numbers[index].Value <= numbers[index - 1].Value)
            {
                throw new InvalidAppendReceipt("sequence numbers are not in append order");
            }

            var context = Convert(received[index], numbers[index], events[index].EventSourceId, eventTypes.GetEventTypeFor(events[index].Event.GetType()), eventStore, @namespace);
            contexts.Add(context);
        }

        return contexts.AsReadOnly();
    }
}
