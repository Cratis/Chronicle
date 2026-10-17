// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// The registrations of clients whose subscription was replaced by a later client registering a different definition,
    /// oldest first. Like <see cref="_subscription"/>, it lives for the activation only.
    /// </summary>
    readonly List<SupersededRegistration> _supersededRegistrations = [];

    /// <summary>
    /// Re-runs the subscription of a target under the definition currently active, as if that client registered again.
    /// Null until a client has registered with this activation.
    /// </summary>
    Func<ObserverSubscriberTarget, Task>? _resubscribeTarget;

    void SupersedeTargetsOtherThan(ConnectionId connectionId)
    {
        _supersededRegistrations.RemoveAll(registration => registration.ConnectionId == connectionId);
        if (_resubscribeTarget is not { } resubscribe)
        {
            return;
        }

        foreach (var target in _subscription.Targets.Where(target => target.ConnectedClient!.ConnectionId != connectionId))
        {
            var connection = target.ConnectedClient!.ConnectionId;
            _supersededRegistrations.RemoveAll(registration => registration.ConnectionId == connection);
            _supersededRegistrations.Add(new(connection, target.SiloAddress, () => resubscribe(target)));
        }
    }

    void DropSupersededRegistration(ConnectionId connectionId) =>
        _supersededRegistrations.RemoveAll(registration => registration.ConnectionId == connectionId);

    void ForgetSupersededRegistrations()
    {
        _supersededRegistrations.Clear();
        _resubscribeTarget = null;
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

            var connectedClients = GrainFactory.GetConnectedClients(candidate.SiloAddress);
            if (!await connectedClients.IsConnected(candidate.ConnectionId))
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

    sealed record SupersededRegistration(ConnectionId ConnectionId, SiloAddress SiloAddress, Func<Task> Resubscribe);
}
