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
    /// <see cref="Concepts.EventSequences.Concurrency.ConcurrencyScopeConjunctions"/>.
    /// </summary>
    /// <param name="scopes">The scopes to convert.</param>
    /// <returns>The ordered guards, or an append error if one source mixes no-check and guarded scopes.</returns>
    public static Result<Concepts.EventSequences.Concurrency.ConcurrencyScopeConjunctions, AppendError> ToChronicle(this IEnumerable<EventSourceConcurrencyScope> scopes)
    {
        var converted = new List<(Concepts.Events.EventSourceId EventSourceId, Concepts.EventSequences.Concurrency.ConcurrencyScope Scope, uint? ScopeIndex)>();
        foreach (var (eventSourceScope, index) in scopes.Select((scope, index) => (scope, index)).Where(_ => !string.IsNullOrWhiteSpace(_.scope.EventSourceId) && _.scope.Scope is not null))
        {
            var eventSourceId = new Concepts.Events.EventSourceId(eventSourceScope.EventSourceId);
            var scope = eventSourceScope.Scope.ToChronicle();
            var existing = converted.Where(_ => _.EventSourceId == eventSourceId).ToArray();
            var isNone = IsNoCheck(scope);
            if (existing.Any(_ => IsNoCheck(_.Scope) != isNone))
            {
                return new AppendError($"Conflicting concurrency scopes for event source '{eventSourceId}'.");
            }

            if (!existing.Any(_ => AreEqual(_.Scope, scope))) converted.Add((eventSourceId, scope, (uint)index));
        }

        return new Concepts.EventSequences.Concurrency.ConcurrencyScopeConjunctions(converted);
    }

    /// <summary>
    /// Converts a validated predicate to its API shape without exposing internal sequence-number sentinels.
    /// </summary>
    /// <param name="scope">The predicate.</param>
    /// <returns>The predicate and explicit absence expectation.</returns>
    internal static ConcurrencyScope ToApi(this Concepts.EventSequences.Concurrency.ConcurrencyScope scope) =>
        new(scope.ExpectsNoMatchingEvent ? Concepts.Events.EventSequenceNumber.Unavailable.Value : scope.SequenceNumber.Value, scope.EventSourceId, scope.EventStreamType?.Value, scope.EventStreamId?.Value, scope.EventSourceType?.Value, scope.EventTypes?.Select(_ => new EventType(_.Id, _.Generation, _.Tombstone)), scope.ExpectsNoMatchingEvent);

    static bool IsNoCheck(Concepts.EventSequences.Concurrency.ConcurrencyScope scope) => scope == Concepts.EventSequences.Concurrency.ConcurrencyScope.None;

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
