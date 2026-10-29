// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_webhook_observer_with_a_waiting_client : a_webhook_observer
{
    protected ConnectedClient _client;

    void Establish()
    {
        _client = new ConnectedClient { ConnectionId = "a-connection", Version = "1.0.0" };
        var namespaces = Substitute.For<INamespaceStorage>();
        namespaces.GetAll().Returns([new NamespaceState(_observerKey.Namespace, DateTimeOffset.UtcNow)]);
        _eventStoreStorage.Namespaces.Returns(namespaces);
    }
}
