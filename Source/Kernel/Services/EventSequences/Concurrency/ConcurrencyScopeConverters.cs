// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Concepts;

namespace Cratis.Chronicle.Services.EventSequences.Concurrency;

/// <summary>
/// Represents methods for converting between <see cref="ConcurrencyScope"/> and <see cref="Cratis.Chronicle.Contracts.EventSequences.Concurrency.ConcurrencyScope"/>.
/// </summary>
internal static class ConcurrencyScopeConverters
{
    /// <summary>
    /// Convert to a Chronicle representation of <see cref="ConcurrencyScope"/> from a contract version of <see cref="Cratis.Chronicle.Contracts.EventSequences.Concurrency.ConcurrencyScope"/>.
    /// </summary>
    /// <param name="scope"><see cref="Cratis.Chronicle.Contracts.EventSequences.Concurrency.ConcurrencyScope"/> to convert.</param>
    /// <returns>A converted <see cref="ConcurrencyScope"/>.</returns>
    public static ConcurrencyScope ToChronicle(
        this Cratis.Chronicle.Contracts.EventSequences.Concurrency.ConcurrencyScope scope) =>
        new(
            scope.EventSequenceNumber,
            scope.EventSourceId,
            ToMaybeConcept<EventStreamType>(scope.EventStreamType, value => value),
            ToMaybeConcept<EventStreamId>(scope.EventStreamId, value => value),
            ToMaybeConcept<EventSourceType>(scope.EventSourceType, value => value),
            scope.EventTypes?.Select(EventType.Parse));

    /// <summary>
    /// Convert to a Chronicle representation of <see cref="ConcurrencyScopes"/> from a contract version of <see cref="IDictionary{TKey,TValue}"/> of <see cref="string"/> and <see cref="Cratis.Chronicle.Contracts.EventSequences.Concurrency.ConcurrencyScopeMany"/>.
    /// </summary>
    /// <param name="scopes"><see cref="IDictionary{TKey,TValue}"/> of <see cref="string"/> and <see cref="Cratis.Chronicle.Contracts.EventSequences.Concurrency.ConcurrencyScopeMany"/> to convert.</param>
    /// <returns>A converted <see cref="ConcurrencyScope"/>.</returns>
    public static ConcurrencyScopes ToChronicle(
        this IDictionary<string, Cratis.Chronicle.Contracts.EventSequences.Concurrency.ConcurrencyScope> scopes)
    {
        var result = new ConcurrencyScopes();
        foreach (var (eventSourceId, scope) in scopes)
        {
            result.Add(eventSourceId, scope.ToChronicle());
        }

        return result;
    }

    static T? ToMaybeConcept<T>(string? value, Func<string, T> toConcept)
        where T : ConceptAs<string>
        => string.IsNullOrEmpty(value) ? null : toConcept(value);
}