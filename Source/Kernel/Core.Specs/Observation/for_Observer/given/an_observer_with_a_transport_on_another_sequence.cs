// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_a_transport_on_another_sequence : an_observer_under_a_mismatching_key
{
    protected ConnectedClient _client;
    protected IObserver _owningObserver;
    protected ObserverKey _owningKey;

    void Establish()
    {
        _client = new ConnectedClient { ConnectionId = "the-connection" };
        _owningKey = _observerKey with { EventSequenceId = _definitionEventSequenceId };
        _owningObserver = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _owningObserver);
        _eventStoreStorage.Observers.Get(_observerId).Returns(_definitionStorage.State);
    }
}
