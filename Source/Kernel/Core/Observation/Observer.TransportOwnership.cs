// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    static bool WebhookWouldReplaceConnectedClient(Type subscriberType, ObserverSubscription subscription) =>
        subscriberType.IsAssignableTo(typeof(IWebhookObserverSubscriber)) && subscription.IsServedByConnectedClient;

    static bool ConnectedClientWouldReplaceWebhook(object? arguments, ObserverSubscription subscription) =>
        arguments is ConnectedClient && subscription.IsServedByWebhook;

    async Task ThrowIfServedByAnotherTransport(Type subscriberType, object? arguments)
    {
        ThrowIfTransportConflicts(subscriberType, arguments, _subscription);
        if (!subscriberType.IsAssignableTo(typeof(IWebhookObserverSubscriber)) && arguments is not ConnectedClient)
        {
            return;
        }

        var owningSubscription = await ObserverTransportSubscriptions.OnOtherSequence(storage, GrainFactory, _observerKey);
        if (owningSubscription is not null)
        {
            ThrowIfTransportConflicts(subscriberType, arguments, owningSubscription);
        }
    }

    void ThrowIfTransportConflicts(Type subscriberType, object? arguments, ObserverSubscription subscription)
    {
        if (WebhookWouldReplaceConnectedClient(subscriberType, subscription) || ConnectedClientWouldReplaceWebhook(arguments, subscription))
        {
            throw new ObserverServedByAnotherTransport(_observerKey);
        }
    }
}
