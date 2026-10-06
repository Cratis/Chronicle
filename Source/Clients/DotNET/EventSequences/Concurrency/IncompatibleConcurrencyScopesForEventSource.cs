// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// The exception that is thrown when a batch appended through event source definitions needs different concurrency
/// guards for the same event source id, which the append protocol cannot represent.
/// </summary>
/// <remarks>
/// An append carries one concurrency scope per event source id. Rather than guarding one of the events and silently
/// leaving the others unguarded, or widening the guard beyond what any definition asked for, the append is rejected
/// before anything is written. Supply an explicit scope for the event source id that suits every event in the batch,
/// or append the events in separate batches.
/// </remarks>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> that the guards compete for.</param>
/// <param name="scopes">The distinct guards that were required.</param>
public class IncompatibleConcurrencyScopesForEventSource(EventSourceId eventSourceId, IReadOnlyList<ConcurrencyScope> scopes)
    : Exception(
        $"The batch requires {scopes.Count} different concurrency guards for event source id '{eventSourceId}', but an append carries only one concurrency scope per event source id: " +
        $"{string.Join("; ", scopes.Select(Describe))}. Nothing was appended. " +
        "Pass an explicit concurrency scope for this event source id that suits every event in the batch, or append the events in separate batches.")
{
    /// <summary>
    /// Gets the <see cref="EventSourceId"/> that the guards compete for.
    /// </summary>
    public EventSourceId EventSourceId { get; } = eventSourceId;

    /// <summary>
    /// Gets the distinct guards that were required.
    /// </summary>
    public IReadOnlyList<ConcurrencyScope> Scopes { get; } = scopes;

    static string Describe(ConcurrencyScope scope) =>
        $"[source id: {scope.EventSourceId?.Value ?? "any"}, source type: {scope.EventSourceType?.Value ?? "any"}, " +
        $"stream type: {scope.EventStreamType?.Value ?? "any"}, stream id: {scope.EventStreamId?.Value ?? "any"}, " +
        $"event types: {(scope.EventTypes?.Any() == true ? string.Join(", ", scope.EventTypes.Select(_ => _.Id.Value)) : "any")}]";
}
