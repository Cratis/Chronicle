// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.Services.EventSequences.Concurrency;

/// <summary>
/// Represents methods for converting between <see cref="ConcurrencyScope"/> and <see cref="Contracts.EventSequences.Concurrency.ConcurrencyScope"/>.
/// </summary>
internal static class ConcurrencyScopeConverters
{
    /// <summary>
    /// Convert to a Chronicle representation of <see cref="ConcurrencyScope"/> from a contract version of <see cref="Contracts.EventSequences.Concurrency.ConcurrencyScope"/>.
    /// </summary>
    /// <param name="scope"><see cref="Contracts.EventSequences.Concurrency.ConcurrencyScope"/> to convert.</param>
    /// <returns>A converted <see cref="ConcurrencyScope"/>.</returns>
    /// <remarks>
    /// The expectation that no event matching the narrowing exists is read from its own field, never inferred from
    /// the sequence number. A client too old to set the field sends the "unavailable" number it always sent, which
    /// becomes the incomplete scope the validator skips and reports as unchecked - the older behavior, not a
    /// silently weaker one.
    /// </remarks>
    public static ConcurrencyScope ToChronicle(
        this Contracts.EventSequences.Concurrency.ConcurrencyScope scope) =>
        new(
            ToExpectedSequenceNumber(scope),
            scope.EventSourceId,
            ToMaybeConcept<EventStreamType>(scope.EventStreamType, value => value),
            ToMaybeConcept<EventStreamId>(scope.EventStreamId, value => value),
            ToMaybeConcept<EventSourceType>(scope.EventSourceType, value => value),
            scope.EventTypes?.ToChronicle());

    /// <summary>
    /// Convert to a Chronicle representation of <see cref="ConcurrencyScopes"/> from a contract version of <see cref="IDictionary{TKey,TValue}"/> of <see cref="string"/> and <see cref="Contracts.EventSequences.Concurrency.ConcurrencyScope"/>.
    /// </summary>
    /// <param name="scopes"><see cref="IDictionary{TKey,TValue}"/> of <see cref="string"/> and <see cref="Contracts.EventSequences.Concurrency.ConcurrencyScope"/> to convert.</param>
    /// <returns>A converted <see cref="ConcurrencyScope"/>.</returns>
    public static ConcurrencyScopes ToChronicle(
        this IDictionary<string, Contracts.EventSequences.Concurrency.ConcurrencyScope>? scopes) =>
        new((scopes ?? new Dictionary<string, Contracts.EventSequences.Concurrency.ConcurrencyScope>())
            .Where(eventSourceIdAndScope =>
                !string.IsNullOrWhiteSpace(eventSourceIdAndScope.Key) &&
                eventSourceIdAndScope.Value is not null)
            .ToDictionary(
                eventSourceIdAndScope => new EventSourceId(eventSourceIdAndScope.Key),
                eventSourceIdAndScope => eventSourceIdAndScope.Value.ToChronicle()));

    /// <summary>
    /// Resolve the expected <see cref="EventSequenceNumber"/> a contract scope asks to be validated against.
    /// </summary>
    /// <param name="scope">The contract scope that arrived.</param>
    /// <returns>The expected <see cref="EventSequenceNumber"/>.</returns>
    /// <remarks>
    /// Only <see cref="Contracts.EventSequences.Concurrency.ConcurrencyScope.ExpectsNoMatchingEvent"/> can put a
    /// scope into the before-first expectation. A sequence number that happens to hold the reserved value without
    /// the field being set did not come from a client that means it, and is downgraded to
    /// <see cref="EventSequenceNumber.Unavailable"/> rather than promoted to an expectation - the number field
    /// carries no intent of its own, which is the property that keeps a version mismatch from inventing a check
    /// or silently dropping one.
    /// </remarks>
    static EventSequenceNumber ToExpectedSequenceNumber(Contracts.EventSequences.Concurrency.ConcurrencyScope scope)
    {
        if (scope.ExpectsNoMatchingEvent)
        {
            return EventSequenceNumber.BeforeFirst;
        }

        var sequenceNumber = new EventSequenceNumber(scope.SequenceNumber);
        return sequenceNumber.IsBeforeFirst ? EventSequenceNumber.Unavailable : sequenceNumber;
    }

    static T? ToMaybeConcept<T>(string? value, Func<string, T> toConcept)
        where T : ConceptAs<string>
        => string.IsNullOrEmpty(value) ? null : toConcept(value);
}
