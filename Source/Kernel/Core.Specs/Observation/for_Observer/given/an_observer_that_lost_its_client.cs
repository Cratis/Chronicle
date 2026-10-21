// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_that_lost_its_client : an_observer
{
    protected static readonly EventType event_type = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    protected ConnectedClient _client;
    protected INamespaceStorage _namespaces;

    void Establish()
    {
        _client = new ConnectedClient { ConnectionId = "a-connection", Version = "1.0.0" };
        _namespaces = Substitute.For<INamespaceStorage>();
        _namespaces.GetAll().Returns([new NamespaceState(_observerKey.Namespace, DateTimeOffset.UtcNow)]);
        _eventStoreStorage.Namespaces.Returns(_namespaces);
    }
}
