// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// The registrations of clients whose subscription was replaced by a later client registering a different definition,
    /// oldest first: the standbys that take over when the client holding the subscription leaves.
    /// </summary>
    /// <remarks>
    /// They are part of the durable subscription (see <see cref="ObserverDurableSubscription.Standbys"/>), so an observer that
    /// holds them can be dormant and has them again when it wakes.
    /// </remarks>
    readonly List<SupersededRegistration> _supersededRegistrations = [];

    /// <summary>
    /// Gets the number of times the standbys changed, which makes the recorded durable subscription stale.
    /// </summary>
    int _supersededRegistrationsVersion;

    /// <summary>
    /// Re-runs the subscription of a target under the definition currently active, as if that client registered again.
    /// Null until a client has registered with this activation.
    /// </summary>
    ResubscribeDefinition? _resubscribe;

    /// <summary>
    /// Gets the standbys of the observer as the durable subscription records them.
    /// </summary>
    /// <returns>The standbys, oldest first, or null when there are none.</returns>
    IReadOnlyList<ObserverDurableSubscriptionStandby>? DurableStandbys() =>
        _supersededRegistrations.Count == 0 ? null : [.. _supersededRegistrations.Select(registration => registration.Standby)];

    void SupersedeTargetsOtherThan(ConnectionId connectionId)
    {
        DropSupersededRegistration(connectionId);
        if (_resubscribe is not { } definition)
        {
            return;
        }

        foreach (var target in _subscription.Targets.Where(target => target.ConnectedClient!.ConnectionId != connectionId))
        {
            var connection = target.ConnectedClient!.ConnectionId;
            DropSupersededRegistration(connection);
            _supersededRegistrations.Add(new(
                connection,
                target.SiloAddress,
                () => definition.Resubscribe(target),
                new(
                    connection.Value,
                    target.SiloAddress.ToParsableString(),
                    definition.EventTypes,
                    definition.IsReplayable,
                    definition.Filters,
                    definition.PinsGenerations,
                    definition.DeliversPages)));
            _supersededRegistrationsVersion++;
        }
    }

    void DropSupersededRegistration(ConnectionId connectionId)
    {
        if (_supersededRegistrations.RemoveAll(registration => registration.ConnectionId == connectionId) > 0)
        {
            _supersededRegistrationsVersion++;
        }
    }

    void ForgetSupersededRegistrations()
    {
        if (_supersededRegistrations.Count > 0)
        {
            _supersededRegistrations.Clear();
            _supersededRegistrationsVersion++;
        }

        _resubscribe = null;
    }

    /// <summary>
    /// Takes the standbys a durable subscription names back as superseded registrations.
    /// </summary>
    /// <param name="record">The durable subscription the observer wakes from.</param>
    /// <remarks>
    /// The standbys are not validated here: a standby whose client has left is found, and skipped, when it is promoted.
    /// </remarks>
    void RestoreSupersededRegistrations(ObserverDurableSubscription record)
    {
        foreach (var standby in record.Standbys ?? [])
        {
            var registration = record.Kind switch
            {
                ObserverSubscriptionKind.ClientReactor when standby.DeliversPages => RegistrationOf<IReactorPageObserverSubscriber>(standby),
                ObserverSubscriptionKind.ClientReactor => RegistrationOf<IReactorObserverSubscriber>(standby),
                ObserverSubscriptionKind.ClientReducer when standby.DeliversPages => RegistrationOf<IReducerPageObserverSubscriber>(standby),
                ObserverSubscriptionKind.ClientReducer => RegistrationOf<IReducerObserverSubscriber>(standby),
                _ => null
            };

            if (registration is not null && !IsKnownClient(registration.ConnectionId))
            {
                _supersededRegistrations.Add(registration);
                _supersededRegistrationsVersion++;
            }
        }
    }

    SupersededRegistration RegistrationOf<TObserverSubscriber>(ObserverDurableSubscriptionStandby standby)
        where TObserverSubscriber : IObserverSubscriber
    {
        var connectionId = new ConnectionId(standby.ConnectionId);
        var silo = SiloAddress.FromParsableString(standby.Silo);
        return new(
            connectionId,
            silo,
            async () =>
            {
                var owner = await ConnectedClientsLocator.Find(GrainFactory, silo, connectionId)
                    ?? throw new InvalidOperationException($"The client {connectionId} of a standby registration is not connected.");
                var connectedClient = await owner.GetConnectedClient(connectionId);
                await SubscribeToEventTypes<TObserverSubscriber>(
                    observerDefinition.State.Type,
                    standby.EventTypes.Select(EventType.Parse).ToArray(),
                    silo,
                    connectedClient,
                    standby.IsReplayable,
                    standby.Filters?.ToObserverFilters(),
                    reactivateRetired: false,
                    automatic: true,
                    generationDelivery: standby.PinsGenerations ? EventGenerationDelivery.Pinned : EventGenerationDelivery.Compatibility);
            },
            standby);
    }

    ResubscribeDefinition CreateResubscribeDefinition<TObserverSubscriber>(
        ObserverType observerType,
        IEnumerable<EventType> eventTypes,
        bool isReplayable,
        ObserverFilters? filters,
        EventGenerationDelivery generationDelivery)
        where TObserverSubscriber : IObserverSubscriber
    {
        var types = eventTypes.ToArray();
        return new(
            existing => SubscribeToEventTypes<TObserverSubscriber>(
                observerType,
                types,
                existing.SiloAddress,
                existing.ConnectedClient,
                isReplayable,
                filters,
                reactivateRetired: false,
                automatic: true,
                generationDelivery: generationDelivery),
            [.. types.Select(eventType => $"{eventType.Id.Value}+{eventType.Generation.Value}+{eventType.Tombstone}")],
            isReplayable,
            ObserverDurableSubscriptionFilters.From(filters),
            generationDelivery == EventGenerationDelivery.Pinned,
            typeof(TObserverSubscriber).IsAssignableTo(typeof(IObserverPageSubscriber)) || typeof(TObserverSubscriber).IsAssignableTo(typeof(IObserverPartitionPageSubscriber)));
    }

    /// <summary>
    /// Hands the observer to the most recently superseded client that is still connected, or unsubscribes when there is none.
    /// </summary>
    /// <remarks>
    /// The promotion is kernel-initiated: it subscribes automatically, so it neither ends a quarantine nor reactivates a retired observer.
    /// Entries of clients that are no longer connected are dropped on the way.
    /// </remarks>
    /// <returns>Awaitable task.</returns>
    async Task PromoteSupersededRegistrationOrUnsubscribe()
    {
        while (_supersededRegistrations.Count > 0)
        {
            var candidate = _supersededRegistrations[^1];
            _supersededRegistrations.RemoveAt(_supersededRegistrations.Count - 1);
            _supersededRegistrationsVersion++;

            if (await ConnectedClientsLocator.Find(GrainFactory, candidate.SiloAddress, candidate.ConnectionId) is null)
            {
                logger.SupersededClientDisconnected(candidate.ConnectionId);
                continue;
            }

            logger.PromotingSupersededClient(candidate.ConnectionId);

            // The active client has left, so it must not take part in the subscription that replaces its own.
            _subscription = _subscription with { Targets = [] };
            try
            {
                await candidate.Resubscribe();
            }
            catch (Exception ex)
            {
                // A candidate that cannot take over must not strand the observer: try the next one, then give up.
                logger.PromotingSupersededClientFailed(candidate.ConnectionId, ex);
                continue;
            }

            // A subscription that was not applied (a retired observer, for instance) still points at the departed client.
            if (_subscription.Targets.Any(target => target.ConnectedClient?.ConnectionId == candidate.ConnectionId))
            {
                return;
            }
        }

        await Unsubscribe();
    }

    sealed record SupersededRegistration(ConnectionId ConnectionId, SiloAddress SiloAddress, Func<Task> Resubscribe, ObserverDurableSubscriptionStandby Standby);

    sealed record ResubscribeDefinition(
        Func<ObserverSubscriberTarget, Task> Resubscribe,
        IReadOnlyList<string> EventTypes,
        bool IsReplayable,
        ObserverDurableSubscriptionFilters? Filters,
        bool PinsGenerations,
        bool DeliversPages);
}
