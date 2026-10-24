// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    static bool IsAffectedByRevision(IConstraintDefinition definition, EventTypeId type) => definition switch
    {
        UniqueConstraintDefinition unique => unique.SupportsEventType(type) || unique.RemovedWith.Contains(type),
        ClosesStreamConstraintDefinition closing => closing.EventTypeIds.Contains(type) || closing.ReopenedBy.Contains(type),
        _ => false
    };

    /// <summary>
    /// Prepares the constraint indexes for revising an event: marks every affected constraint pending from the
    /// event's position, persists the lowered watermark, and releases the keys the old content produced that the
    /// refold cannot reach. The existing repair then refolds from the revised position.
    /// </summary>
    /// <param name="event">The event as it is stored before the revision.</param>
    /// <returns>Awaitable task.</returns>
    async Task PrepareConstraintIndexesForRevision(AppendedEvent @event)
    {
        var type = @event.Context.EventType.Id;
        var position = @event.Context.SequenceNumber;
        var affected = _indexedDefinitions.Values.Where(definition => IsAffectedByRevision(definition, type)).ToArray();
        if (affected.Length == 0) return;

        foreach (var definition in affected)
        {
            MarkConstraintIndexPending(definition.Name, position);
        }
        UpdateConstraintIndexWatermark();
        _constraintIndexReplay = null;
        await WriteStateAsync();

        // Only entries stamped at exactly this position can belong to the old content; anything else was claimed by
        // another event and must be left alone.
        var plaintext = await CatchUpContent().Release(@event);
        var context = new ConstraintValidationContext([], @event.Context.EventSourceId, type, plaintext, @event.Context.EventSourceType, @event.Context.EventStreamType, @event.Context.EventStreamId);
        foreach (var definition in affected)
        {
            if (definition is UniqueConstraintDefinition { Mode: UniqueConstraintMode.PerValue } unique) await ReleaseRevisedClaim(unique, context, position);
            else if (definition is ClosesStreamConstraintDefinition { EventStreamIdFrom: not null } closing) await ReopenRevisedClosures(closing, position);
        }
    }

    async Task ReleaseRevisedClaim(UniqueConstraintDefinition definition, ConstraintValidationContext context, EventSequenceNumber position)
    {
        if (!definition.SupportsEventType(context.EventTypeId) || definition.RemovedWith.Contains(context.EventTypeId)) return;
        var values = definition.GetPropertiesAndValues(context).ToArray();
        if (values.Length == 0) return;
        var claims = storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace).GetUniqueConstraintsStorage(_eventSequenceId);
        var scope = definition.Scope.BuildScopeKey(context.EventSourceType, context.EventStreamType, context.EventStreamId);
        var value = new UniqueConstraintValue(values.GetValue(definition.IgnoreCasing));
        var (isOwner, claimedAt) = await claims.IsAllowed(context.EventSourceId, definition, value, scope);
        if (isOwner && claimedAt == position) await claims.RemoveValue(context.EventSourceId, definition, value, scope);
    }

    async Task ReopenRevisedClosures(ClosesStreamConstraintDefinition definition, EventSequenceNumber position)
    {
        // The refold only visits the scope the new content resolves to, so closures the old content decided are
        // reopened here and the repair closes the new scope again.
        foreach (var closure in (await ClosedStreamsStorage.GetForEvent(definition.Name.Value, position)).ToArray())
        {
            await ClosedStreamsStorage.Reopen(definition.Name.Value, closure.Scope);
        }
    }
}
