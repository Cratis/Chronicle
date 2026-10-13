// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converters between <see cref="ConcurrencyScope"/> and its Chronicle representation.
/// </summary>
internal static class ConcurrencyScopeConverters
{
    /// <summary>
    /// Converts a contract <see cref="Contracts.Sequences.ConcurrencyScope"/> to a <see cref="ConcurrencyScope"/>.
    /// </summary>
    /// <param name="scope">The contract scope to convert.</param>
    /// <returns>The converted scope.</returns>
    public static ConcurrencyScope ToApi(this Contracts.Sequences.ConcurrencyScope scope) =>
        new(
            scope.SequenceNumber,
            scope.EventSourceId,
            scope.EventStreamType,
            scope.EventStreamId,
            scope.EventSourceType,
            scope.EventTypes?.Select(_ => _.ToApi()),
            scope.ExpectsNoMatchingEvent);

    /// <summary>
    /// Converts a contract <see cref="Contracts.Sequences.EventSourceConcurrencyScope"/> to an
    /// <see cref="EventSourceConcurrencyScope"/>.
    /// </summary>
    /// <param name="scope">The contract scope to convert.</param>
    /// <returns>The converted scope.</returns>
    public static EventSourceConcurrencyScope ToApi(this Contracts.Sequences.EventSourceConcurrencyScope scope) =>
        new(scope.EventSourceId, scope.Scope.ToApi());

    /// <summary>
    /// Converts a <see cref="ConcurrencyScope"/> to a <see cref="Concepts.EventSequences.Concurrency.ConcurrencyScope"/>.
    /// </summary>
    /// <param name="scope">The scope to convert.</param>
    /// <returns>The converted scope.</returns>
    /// <remarks>
    /// The expectation that no event matching the narrowing exists is read from its own field, never inferred from
    /// the sequence number. A caller too old to set the field sends the "unavailable" number it always sent, which
    /// becomes the incomplete scope the validator skips and reports as unchecked - the older behavior, not a
    /// silently weaker one.
    /// </remarks>
    public static Concepts.EventSequences.Concurrency.ConcurrencyScope ToChronicle(this ConcurrencyScope scope) =>
        new(
            ToExpectedSequenceNumber(scope),
            scope.EventSourceId,
            ToMaybeConcept<Concepts.Events.EventStreamType>(scope.EventStreamType, value => value),
            ToMaybeConcept<Concepts.Events.EventStreamId>(scope.EventStreamId, value => value),
            ToMaybeConcept<Concepts.Events.EventSourceType>(scope.EventSourceType, value => value),
            scope.EventTypes?.Select(_ => _.ToChronicle()));

    /// <summary>
    /// Converts a collection of <see cref="EventSourceConcurrencyScope"/> to a
    /// <see cref="Concepts.EventSequences.Concurrency.ConcurrencyScopes"/>.
    /// </summary>
    /// <param name="scopes">The scopes to convert.</param>
    /// <returns>The converted scopes, or an append error if one event source carries conflicting scopes.</returns>
    public static Result<Concepts.EventSequences.Concurrency.ConcurrencyScopes, AppendError> ToChronicle(this IEnumerable<EventSourceConcurrencyScope> scopes)
    {
        var converted = new Dictionary<Concepts.Events.EventSourceId, Concepts.EventSequences.Concurrency.ConcurrencyScope>();
        foreach (var eventSourceScope in scopes.Where(scope => !string.IsNullOrWhiteSpace(scope.EventSourceId) && scope.Scope is not null))
        {
            var eventSourceId = new Concepts.Events.EventSourceId(eventSourceScope.EventSourceId);
            var scope = eventSourceScope.Scope.ToChronicle();
            if (converted.TryGetValue(eventSourceId, out var existing) && !AreEqual(existing, scope))
            {
                return new AppendError($"Conflicting concurrency scopes for event source '{eventSourceId}'.");
            }

            converted[eventSourceId] = scope;
        }

        return new Concepts.EventSequences.Concurrency.ConcurrencyScopes(converted);
    }

    static bool AreEqual(Concepts.EventSequences.Concurrency.ConcurrencyScope first, Concepts.EventSequences.Concurrency.ConcurrencyScope second) =>
        first with { EventTypes = null } == second with { EventTypes = null } &&
        (first.EventTypes is null
            ? second.EventTypes is null
            : second.EventTypes is not null && first.EventTypes.ToHashSet().SetEquals(second.EventTypes));

    /// <summary>
    /// Resolve the expected <see cref="Concepts.Events.EventSequenceNumber"/> a scope asks to be validated against.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <returns>The expected <see cref="Concepts.Events.EventSequenceNumber"/>.</returns>
    static Concepts.Events.EventSequenceNumber ToExpectedSequenceNumber(ConcurrencyScope scope)
    {
        if (scope.ExpectsNoMatchingEvent)
        {
            return Concepts.Events.EventSequenceNumber.BeforeFirst;
        }

        var sequenceNumber = new Concepts.Events.EventSequenceNumber(scope.SequenceNumber);
        return sequenceNumber.IsBeforeFirst ? Concepts.Events.EventSequenceNumber.Unavailable : sequenceNumber;
    }

    static T? ToMaybeConcept<T>(string? value, Func<string, T> toConcept)
        where T : ConceptAs<string>
        => string.IsNullOrEmpty(value) ? null : toConcept(value);
}
