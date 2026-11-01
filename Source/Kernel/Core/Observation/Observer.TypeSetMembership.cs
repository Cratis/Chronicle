// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Gets whether the observer's event types are owned by a committed type set. Observers without one behave exactly as before.
    /// </summary>
    bool IsTypeSetControlled => State.TypeSetControl is not null;

    /// <summary>
    /// Gets the event types the observer reads: the committed members when controlled, otherwise the definition's.
    /// </summary>
    IEnumerable<EventType> MembershipEventTypes => State.TypeSetControl is { } control
        ? control.Members.Keys.Select(id => Definition.EventTypes.FirstOrDefault(eventType => eventType.Id == id) ?? new EventType(id, 1)).ToArray()
        : Definition.EventTypes;

    /// <summary>
    /// Applies a client subscription to a type-set controlled observer as a capability-declaring target.
    /// </summary>
    /// <typeparam name="TObserverSubscriber">The subscriber type.</typeparam>
    /// <param name="eventTypes">The event types the client declares.</param>
    /// <param name="siloAddress">The silo of the client.</param>
    /// <param name="subscriberArgs">The subscriber arguments.</param>
    /// <param name="isReplayable">Whether the observer is replayable.</param>
    /// <param name="filters">The observer filters.</param>
    /// <param name="generationDelivery">The generation delivery the client declares.</param>
    /// <returns>The new target, or null when the observer is not controlled or the subscriber is not a connected client.</returns>
    ObserverSubscriberTarget? ApplyControlledSubscription<TObserverSubscriber>(
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs,
        bool isReplayable,
        ObserverFilters? filters,
        EventGenerationDelivery generationDelivery)
        where TObserverSubscriber : IObserverSubscriber
    {
        if (State.TypeSetControl is not { } control || subscriberArgs is not ConnectedClient client) return null;

        var declared = eventTypes.Where(eventType => control.Members.ContainsKey(eventType.Id)).ToArray();
        var ignored = eventTypes.Count() - declared.Length;
        if (ignored > 0) logger.IgnoringNonMemberEventTypes(ignored);

        var target = new ObserverSubscriberTarget(siloAddress, client)
        {
            DeclaredEventTypes = declared,
            GenerationDelivery = generationDelivery
        };
        var current = _subscription.IsSubscribed && _subscription.SubscriberType == typeof(TObserverSubscriber) ? _subscription.Targets : [];
        var targets = current
            .Where(existing => existing.ConnectedClient?.ConnectionId != client.ConnectionId)
            .Append(target)
            .OrderBy(existing => existing.ConnectedClient?.ConnectionId.Value)
            .ToArray();
        _subscription = new(
            _observerId,
            _observerKey,
            MembershipEventTypes,
            typeof(TObserverSubscriber),
            targets[0].SiloAddress,
            targets[0].ConnectedClient,
            isReplayable,
            filters)
        {
            Targets = targets,
            GenerationDelivery = generationDelivery,
            TypeSetRevision = control.Revision,
            TypeSetMembers = control.Members.ToDictionary(member => member.Key, member => member.Value)
        };
        return target;
    }

    /// <summary>
    /// Starts recovery of the partitions held waiting for a handler the new target declares.
    /// </summary>
    /// <param name="target">The target that joined.</param>
    /// <returns>Awaitable task.</returns>
    async Task ResumeHeldPartitionsFor(ObserverSubscriberTarget target)
    {
        var declared = target.DeclaredEventTypes?.Select(eventType => eventType.Id).ToHashSet() ?? [];
        foreach (var partition in Failures.Partitions.Where(failure => failure.Hold is { } hold && declared.Contains(hold.AwaitedEventType) && !failure.IsQuarantined).ToArray())
        {
            await TryRecoverFailedPartition(partition.Partition);
        }
    }
}
