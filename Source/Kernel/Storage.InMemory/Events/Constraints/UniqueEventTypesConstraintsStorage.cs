// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Represents an in-memory implementation of <see cref="IUniqueEventTypesConstraintsStorage"/>.
/// </summary>
/// <remarks>
/// Queries the associated <see cref="EventSequenceStorage"/> to check whether an event of
/// the given type has already been appended for a specific event source - mirroring how the
/// MongoDB implementation reads the event sequence collection directly.
/// </remarks>
/// <param name="eventSequenceStorage">The <see cref="EventSequenceStorage"/> to query.</param>
public class UniqueEventTypesConstraintsStorage(
    EventSequenceStorage eventSequenceStorage) : IUniqueEventTypesConstraintsStorage
{
    const string FlattenedScopeKeyNotSupported =
        "This storage narrows a scoped unique event type constraint by typed dimensions and cannot answer a flattened scope key. Call IsAllowedWithinScope with a ResolvedConstraintScope instead.";

    /// <inheritdoc/>
    /// <remarks>
    /// Kept so that a caller compiled against the original interface still binds. An empty or absent key means
    /// nothing is narrowed, which is exactly an unscoped typed lookup and is answered as one. A non-empty key is
    /// refused rather than interpreted: this storage decides scope from typed dimension values, and a flattened
    /// key cannot be turned back into them without reintroducing the aliasing it exists to avoid. Refusing is a
    /// behavior change for a caller that used to pass a real key straight to this class - that call was silently
    /// answered against the wrong cycle before, so there is no correct behavior to preserve.
    /// <para>
    /// The key never reaches the message. It is built from event source type, stream type and stream id, which are
    /// caller data and potentially personal.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">Thrown when a non-empty flattened scope key is supplied.</exception>
    public Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(
        UniqueEventTypeConstraintDefinition definition,
        EventSourceId eventSourceId,
        string scopeKey = "") =>
        string.IsNullOrEmpty(scopeKey)
            ? IsAllowedWithinScope(definition, eventSourceId)
            : throw new NotSupportedException(FlattenedScopeKeyNotSupported);

    /// <inheritdoc/>
    public Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowedWithinScope(
        UniqueEventTypeConstraintDefinition definition,
        EventSourceId eventSourceId,
        ResolvedConstraintScope? scope = null)
    {
        var coveredEventTypeIds = definition.EventTypeIds.ToHashSet();
        var forEventSource = eventSequenceStorage.Events
            .Where(_ => _.Context.EventSourceId == eventSourceId && IsWithinScope(scope, _.Context))
            .ToArray();

        var latestRemoval = GetLatestRemoval(definition, forEventSource);

        // Ordered so the sequence number reported back is the covered event that actually holds the cycle, rather
        // than whichever one the sequence happened to yield first.
        var existing = forEventSource
            .Where(_ => coveredEventTypeIds.Contains(_.Context.EventType.Id) &&
                        (latestRemoval is null || _.Context.SequenceNumber.Value > latestRemoval.Value))
            .OrderBy(_ => _.Context.SequenceNumber.Value)
            .FirstOrDefault();

        if (existing is not null)
        {
            return Task.FromResult((false, existing.Context.SequenceNumber));
        }

        return Task.FromResult((true, EventSequenceNumber.Unavailable));
    }

    /// <summary>
    /// Find the most recent event on the event source that releases the constraint.
    /// </summary>
    /// <param name="definition">The <see cref="UniqueEventTypeConstraintDefinition"/> to read the removal events from.</param>
    /// <param name="forEventSource">The events already appended for the event source being answered for.</param>
    /// <returns>The <see cref="EventSequenceNumber"/> the current cycle starts after, or <see langword="null"/> when nothing released it.</returns>
    /// <remarks>
    /// Any of the declared removal events ends a cycle, so the latest across all of them is the one that counts —
    /// looking at only one of them would keep answering against a cycle that another terminal fact already closed.
    /// </remarks>
    static EventSequenceNumber? GetLatestRemoval(UniqueEventTypeConstraintDefinition definition, IEnumerable<AppendedEvent> forEventSource)
    {
        var removalEventTypeIds = definition.RemovedWith.ToHashSet();
        if (removalEventTypeIds.Count == 0)
        {
            return null;
        }

        var removals = forEventSource
            .Where(_ => removalEventTypeIds.Contains(_.Context.EventType.Id))
            .Select(_ => _.Context.SequenceNumber.Value)
            .ToArray();

        return removals.Length == 0 ? null : removals.Max();
    }

    /// <summary>
    /// Check whether an already-appended event falls within the same scope as the event being validated.
    /// </summary>
    /// <param name="scope">The <see cref="ResolvedConstraintScope"/> of the event being validated, or <see langword="null"/> when unscoped.</param>
    /// <param name="context">The <see cref="EventContext"/> of the already-appended event being considered.</param>
    /// <returns>True if the event is within scope, false if it belongs to a different scope.</returns>
    /// <remarks>
    /// Every dimension is compared as its own typed value, mirroring the equality predicate the persistent
    /// providers push into the database. A dimension the constraint is not scoped by is <see langword="null"/> and
    /// narrows nothing.
    /// </remarks>
    static bool IsWithinScope(ResolvedConstraintScope? scope, EventContext context) =>
        scope is null ||
        ((scope.EventSourceType is null || context.EventSourceType == scope.EventSourceType) &&
        (scope.EventStreamType is null || context.EventStreamType == scope.EventStreamType) &&
        (scope.EventStreamId is null || context.EventStreamId == scope.EventStreamId));
}
