// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Describes the minimum safe evolution operation for one namespace.
/// </summary>
/// <param name="Operation">The operation to perform.</param>
/// <param name="AffectedEventSources">Event sources to rebuild for a partial replay.</param>
/// <param name="AffectedEventTypes">Event types to apply during a partial replay.</param>
internal record DefinitionEvolutionPlan(
    DefinitionEvolutionOperation Operation,
    IReadOnlyCollection<EventSourceId> AffectedEventSources,
    IReadOnlyCollection<EventType> AffectedEventTypes)
{
    /// <summary>
    /// Gets a plan that performs no work.
    /// </summary>
    public static readonly DefinitionEvolutionPlan NoAction = new(DefinitionEvolutionOperation.NoAction, [], []);

    /// <summary>
    /// Gets a plan that rebuilds the complete observer.
    /// </summary>
    public static readonly DefinitionEvolutionPlan FullReplay = new(DefinitionEvolutionOperation.FullReplay, [], []);

    /// <summary>
    /// Determines a plan for a reducer change that only adds event types.
    /// </summary>
    /// <param name="eventSequence">The event sequence containing existing history.</param>
    /// <param name="addedEventTypes">The event types added by the definition.</param>
    /// <param name="existingEventTypes">The event types the previous definition consumed.</param>
    /// <param name="canReplayPerEventSource">Whether reducer instances are isolated by event source.</param>
    /// <returns>The minimum safe plan.</returns>
    public static async Task<DefinitionEvolutionPlan> ForAddedReducerEventTypes(
        IEventSequenceStorage eventSequence,
        IEnumerable<EventType> addedEventTypes,
        IEnumerable<EventType> existingEventTypes,
        bool canReplayPerEventSource)
    {
        var addedTypes = addedEventTypes.ToArray();
        var tail = await eventSequence.GetTailSequenceNumber(addedTypes);
        if (!tail.IsActualValue)
        {
            return NoAction;
        }

        if (!canReplayPerEventSource)
        {
            return FullReplay;
        }

        var existingTypes = existingEventTypes.ToArray();
        var addedTypeSet = addedTypes.ToHashSet();
        using var cursor = await eventSequence.GetFromSequenceNumber(
            EventSequenceNumber.First,
            eventTypes: existingTypes.Concat(addedTypes));
        var eventsBySource = new Dictionary<EventSourceId, List<AppendedEvent>>();
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                if (!eventsBySource.TryGetValue(@event.Context.EventSourceId, out var events))
                {
                    events = [];
                    eventsBySource[@event.Context.EventSourceId] = events;
                }
                events.Add(@event);
            }
        }

        var affectedEventSources = new List<EventSourceId>();
        foreach (var (eventSourceId, events) in eventsBySource)
        {
            var addedEvents = events.FindAll(@event => addedTypeSet.Contains(@event.Context.EventType));
            if (addedEvents.Count == 0)
            {
                continue;
            }

            EventSequenceNumber firstAdded = addedEvents.Min(@event => @event.Context.SequenceNumber.Value);
            if (events.Exists(@event => !addedTypeSet.Contains(@event.Context.EventType) && @event.Context.SequenceNumber > firstAdded))
            {
                return FullReplay;
            }
            affectedEventSources.Add(eventSourceId);
        }

        return affectedEventSources.Count == 0
            ? NoAction
            : new(DefinitionEvolutionOperation.PartialReplay, affectedEventSources, addedTypes);
    }

    /// <summary>
    /// Determines a plan for a change that only adds event types.
    /// </summary>
    /// <param name="eventSequence">The event sequence containing existing history.</param>
    /// <param name="addedEventTypes">The event types added by the definition.</param>
    /// <param name="canReplayPerEventSource">Whether the definition proves event sources are independent.</param>
    /// <returns>The minimum safe plan.</returns>
    public static async Task<DefinitionEvolutionPlan> ForAddedEventTypes(
        IEventSequenceStorage eventSequence,
        IEnumerable<EventType> addedEventTypes,
        bool canReplayPerEventSource)
    {
        var eventTypes = addedEventTypes.ToArray();
        var tail = await eventSequence.GetTailSequenceNumber(eventTypes);
        if (!tail.IsActualValue)
        {
            return NoAction;
        }

        if (!canReplayPerEventSource)
        {
            return FullReplay;
        }

        using var cursor = await eventSequence.GetFromSequenceNumber(EventSequenceNumber.First, eventTypes: eventTypes);
        var eventSources = new HashSet<EventSourceId>();
        while (await cursor.MoveNext())
        {
            eventSources.UnionWith(cursor.Current.Select(@event => @event.Context.EventSourceId));
        }

        return eventSources.Count == 0
            ? NoAction
            : new(DefinitionEvolutionOperation.PartialReplay, eventSources, eventTypes);
    }
}
