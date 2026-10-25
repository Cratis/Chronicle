// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    void ThrowIfServedByAnotherTransport(Type subscriberType, object? arguments)
    {
        if (WebhookWouldReplaceConnectedClient(subscriberType) || ConnectedClientWouldReplaceWebhook(arguments))
        {
            throw new ObserverServedByAnotherTransport(_observerKey);
        }
    }

    bool WebhookWouldReplaceConnectedClient(Type subscriberType) =>
        subscriberType.IsAssignableTo(typeof(IWebhookObserverSubscriber)) && _subscription.IsServedByConnectedClient;

    bool ConnectedClientWouldReplaceWebhook(object? arguments) =>
        arguments is ConnectedClient && _subscription.IsServedByWebhook;
}
