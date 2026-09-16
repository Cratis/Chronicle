// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Validates persisted append metadata before exposing any successful local notification.
/// </summary>
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
        Contracts.EventSequences.AppendReceipt? receipt,
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
            receipt.EventSourceId != source.Value || receipt.EventTypeId != eventType.Id.Value ||
            receipt.Generation != eventType.Generation.Value || receipt.Tombstone != eventType.Tombstone)
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
            receipt.Tags?.Any(_ => _ is null) != false || receipt.Causation is null ||
            !DateTimeOffset.TryParse(receipt.Occurred?.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var occurred))
        {
            throw new InvalidAppendReceipt("persisted context is incomplete or malformed");
        }

        return new(
            new(receipt.EventTypeId, receipt.Generation, receipt.Tombstone),
            receipt.EventSourceType,
            receipt.EventSourceId,
            receipt.EventStreamType,
            receipt.EventStreamId,
            receipt.SequenceNumber,
            occurred,
            receipt.EventStore,
            receipt.Namespace,
            receipt.CorrelationId,
            receipt.Causation.Select(ConvertCausation).ToArray(),
            ConvertIdentity(receipt.CausedBy),
            receipt.Tags.Select(_ => (Tag)_).ToArray(),
            receipt.Hash,
            (EventObservationState)receipt.ObservationState,
            receipt.Subject);
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
        IEnumerable<Contracts.EventSequences.AppendReceipt> receipts,
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

    static Causation ConvertCausation(Contracts.EventSequences.AppendCausation causation)
    {
        if (causation is null || causation.Type is null ||
            causation.Properties?.Any(_ => _.Value is null) != false ||
            !DateTimeOffset.TryParse(causation.Occurred?.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var occurred))
        {
            throw new InvalidAppendReceipt("persisted causation is incomplete or malformed");
        }

        return new(occurred, causation.Type, new Dictionary<string, string>(causation.Properties));
    }

    static Identity ConvertIdentity(Contracts.EventSequences.AppendIdentity identity)
    {
        if (identity is null || identity.Subject is null || identity.Name is null || identity.UserName is null)
        {
            throw new InvalidAppendReceipt("persisted identity is incomplete or malformed");
        }

        return new(identity.Subject, identity.Name, identity.UserName, identity.OnBehalfOf is null ? null : ConvertIdentity(identity.OnBehalfOf));
    }
}
