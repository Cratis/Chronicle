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
    public static ConcurrencyScope ToChronicle(
        this Contracts.EventSequences.Concurrency.ConcurrencyScope scope) =>
        new(
            scope.SequenceNumber,
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
        this IDictionary<string, Contracts.EventSequences.Concurrency.ConcurrencyScope> scopes) =>
        new(scopes.ToDictionary(
            eventSourceIdAndScope => new EventSourceId(eventSourceIdAndScope.Key),
            eventSourceIdAndScope => eventSourceIdAndScope.Value.ToChronicle()));

    static T? ToMaybeConcept<T>(string? value, Func<string, T> toConcept)
        where T : ConceptAs<string>
        => string.IsNullOrEmpty(value) ? null : toConcept(value);
}
