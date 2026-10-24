// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    (ConnectionId ConnectionId, string TargetSilo, string Reason, ObserverRunningState RunningState, bool IsSubscribed)? _lastClientRestorationRejection;
    (ConnectionId ConnectionId, string TargetSilo, ObserverRunningState RunningState, bool IsSubscribed)? _lastClientRestorationAttempt;
    (ConnectionId ConnectionId, string TargetSilo, ObserverRunningState RunningState, bool IsSubscribed)? _lastClientRestorationResult;

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
        if (IsKnownClient(connectedClient.ConnectionId))
        {
            LogClientRestorationRejection(connectedClient.ConnectionId, siloAddress, "Client already known");
            return;
        }
        if (CannotReconcileSubscription())
        {
            LogClientRestorationRejection(connectedClient.ConnectionId, siloAddress, "Reconciliation prohibited by observer lifecycle");
            return;
        }

        // A namespace removed while its client is still running must not be brought back by restoring an observer in it.
        var namespaces = await storage.GetEventStore(_observerKey.EventStore).Namespaces.GetAll();
        if (!namespaces.Any(@namespace => @namespace.Name == _observerKey.Namespace))
        {
            LogClientRestorationRejection(connectedClient.ConnectionId, siloAddress, "Namespace no longer exists");
            return;
        }
        if (IsKnownClient(connectedClient.ConnectionId))
        {
            LogClientRestorationRejection(connectedClient.ConnectionId, siloAddress, "Client became known during namespace lookup");
            return;
        }

        _lastClientRestorationRejection = null;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.RestoringClientSubscription(connectedClient.ConnectionId, siloAddress.ToParsableString(), State.RunningState, _subscription.IsSubscribed);
        LogClientRestorationAttempt(connectedClient.ConnectionId, siloAddress);

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
        LogClientRestorationResult(connectedClient.ConnectionId, siloAddress);
    }

    void LogClientRestorationAttempt(ConnectionId connectionId, SiloAddress siloAddress)
    {
        var diagnostic = (ConnectionId: connectionId, TargetSilo: siloAddress.ToParsableString(), State.RunningState, _subscription.IsSubscribed);
        if (_lastClientRestorationAttempt == diagnostic) return;
        _lastClientRestorationAttempt = diagnostic;
        logger.ClientRestorationAttempt(connectionId, diagnostic.TargetSilo, diagnostic.RunningState, diagnostic.IsSubscribed);
    }

    void LogClientRestorationResult(ConnectionId connectionId, SiloAddress siloAddress)
    {
        var diagnostic = (ConnectionId: connectionId, TargetSilo: siloAddress.ToParsableString(), State.RunningState, _subscription.IsSubscribed);
        if (_lastClientRestorationResult == diagnostic) return;
        _lastClientRestorationResult = diagnostic;
        logger.ClientRestorationResult(connectionId, diagnostic.TargetSilo, diagnostic.RunningState, diagnostic.IsSubscribed);
    }

    void LogClientRestorationRejection(ConnectionId connectionId, SiloAddress siloAddress, string reason)
    {
        var diagnostic = (ConnectionId: connectionId, TargetSilo: siloAddress.ToParsableString(), Reason: reason, State.RunningState, _subscription.IsSubscribed);
        if (_lastClientRestorationRejection == diagnostic) return;
        _lastClientRestorationRejection = diagnostic;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.ClientRestorationRejected(connectionId, diagnostic.TargetSilo, reason, diagnostic.RunningState, diagnostic.IsSubscribed);
    }

    bool IsKnownClient(ConnectionId connectionId) =>
        _subscription.Targets.Any(target => target.ConnectedClient?.ConnectionId == connectionId) ||
        _supersededRegistrations.Exists(registration => registration.ConnectionId == connectionId);
}
