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
    public Task<bool> IsClientTarget(ConnectionId connectionId) => Task.FromResult(IsTarget(connectionId));

    /// <inheritdoc/>
    public async Task RestoreClientSubscription<TObserverSubscriber>(
        ObserverType type,
        EventTypeSet eventTypes,
        SiloAddress siloAddress,
        ConnectedClient connectedClient,
        bool isReplayable = true,
        ObserverFilters? filters = default,
        EventGenerationDelivery generationDelivery = EventGenerationDelivery.Compatibility)
        where TObserverSubscriber : IObserverSubscriber
    {
        // Membership can survive failed setup. Retry the current target, but never promote a superseded registration.
        if (_subscription.IsServedByWebhook)
        {
            logger.ClientSubscriptionWaitingForWebhook(_observerKey);
            return;
        }
        if (NeedsNoClientRestoration(connectedClient.ConnectionId) || CannotReconcileSubscription()) return;

        // A namespace removed while its client is still running must not be brought back by restoring an observer in it.
        var namespaces = await storage.GetEventStore(_observerKey.EventStore).Namespaces.GetAll();
        if (!namespaces.Any(@namespace => @namespace.Name == _observerKey.Namespace)) return;
        if (NeedsNoClientRestoration(connectedClient.ConnectionId)) return;

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

    bool NeedsNoClientRestoration(ConnectionId connectionId) =>
        IsKnownClient(connectionId) && (!IsTarget(connectionId) || !_subscriptionSetupFailed);

    bool IsTarget(ConnectionId connectionId) =>
        _subscription.Targets.Any(target => target.ConnectedClient?.ConnectionId == connectionId);

    bool IsKnownClient(ConnectionId connectionId) =>
        IsTarget(connectionId) ||
        _supersededRegistrations.Exists(registration => registration.ConnectionId == connectionId);
}
