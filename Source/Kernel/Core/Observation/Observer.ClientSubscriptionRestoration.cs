// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public Task<bool> KnowsClient(ConnectionId connectionId) => Task.FromResult(IsKnownClient(connectionId));

    /// <inheritdoc/>
    public async Task RestoreClientSubscription<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        ConnectedClient connectedClient,
        bool isReplayable = true,
        ObserverFilters? filters = default,
        EventGenerationDelivery generationDelivery = EventGenerationDelivery.Compatibility)
        where TObserverSubscriber : IObserverSubscriber
    {
        // The subscription and the superseded registrations live for the activation only. A client this activation
        // already knows about - as a target or as a registration a later one replaced - has nothing to restore.
        if (IsKnownClient(connectedClient.ConnectionId) || CannotReconcileSubscription()) return;

        // A namespace removed while its client is still running must not be brought back by restoring an observer in it.
        var namespaces = await storage.GetEventStore(_observerKey.EventStore).Namespaces.GetAll();
        if (!namespaces.Any(@namespace => @namespace.Name == _observerKey.Namespace)) return;
        if (IsKnownClient(connectedClient.ConnectionId)) return;

        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.RestoringClientSubscription(connectedClient.ConnectionId);

        // Restoring is kernel-initiated, exactly like reconciliation: it neither ends a quarantine nor revives a
        // retired observer, and it keeps live progress instead of reloading a stored position.
        await ReconcileSubscription<TObserverSubscriber>(
            type,
            eventTypes,
            siloAddress,
            subscribesToAllEvents: false,
            generationDelivery,
            connectedClient,
            isReplayable,
            filters);
    }

    bool IsKnownClient(ConnectionId connectionId) =>
        _subscription.Targets.Any(target => target.ConnectedClient?.ConnectionId == connectionId) ||
        _supersededRegistrations.Exists(registration => registration.ConnectionId == connectionId);
}
