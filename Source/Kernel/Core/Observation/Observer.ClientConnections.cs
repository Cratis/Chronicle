// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task UnsubscribeIfMatchesClient(ConnectionId connectionId)
    {
        // Single-threaded grain — the check and the subscription change form an atomic action.
        // If a new client has already replaced the subscription, the old client's
        // disconnect cleanup must not tear down the new client's subscription.
        if (await TryHandleClientDisconnectionFromTargets(connectionId)) return;

        if (IsSubscriptionForAnotherClient(connectionId))
        {
            LogClientDisconnected(connectionId, "not the subscribed client, dropping any superseded registration");
            DropSupersededRegistration(connectionId);
            return;
        }

        if (IsSubscriptionWithoutClient())
        {
            // Served by something that is not a connected client instance (a webhook, for example), so a connection
            // identifier says nothing about it. Only the registration that connection superseded can go.
            LogClientDisconnected(connectionId, "not served by a connected client, dropping any superseded registration");
            DropSupersededRegistration(connectionId);
            return;
        }

        LogClientDisconnected(connectionId, "unsubscribing");
        await Unsubscribe();
    }

    async Task<bool> TryHandleClientDisconnectionFromTargets(ConnectionId connectionId)
    {
        if (!(_subscription.IsSubscribed && _subscription.Targets.Count > 0)) return false;
        var remaining = _subscription.Targets
            .Where(target => target.ConnectedClient!.ConnectionId != connectionId)
            .ToArray();
        if (remaining.Length == _subscription.Targets.Count)
        {
            LogClientDisconnected(connectionId, "not a current target, dropping any superseded registration");
            DropSupersededRegistration(connectionId);
            return true;
        }

        if (remaining.Length > 0)
        {
            LogClientDisconnected(connectionId, "removing it from the fan-out targets");
            _subscription = _subscription with
            {
                SiloAddress = remaining[0].SiloAddress,
                Arguments = remaining[0].ConnectedClient,
                Targets = remaining
            };
            return true;
        }

        LogClientDisconnected(connectionId, "last target, promoting a superseded registration or unsubscribing");
        await PromoteSupersededRegistrationOrUnsubscribe();
        return true;
    }

    bool IsSubscriptionForAnotherClient(ConnectionId connectionId) =>
        _subscription.IsSubscribed &&
        _subscription.Arguments is ConnectedClient connectedClient &&
        connectedClient.ConnectionId != connectionId;

    bool IsSubscriptionWithoutClient() => _subscription.IsSubscribed && _subscription.Arguments is not ConnectedClient;
}
