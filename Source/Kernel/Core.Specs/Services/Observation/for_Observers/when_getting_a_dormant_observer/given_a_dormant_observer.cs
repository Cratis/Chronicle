// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_getting_a_dormant_observer;

public class given_a_dormant_observer : given.a_case_sensitive_observer_store
{
    protected IDormantObservers _dormantObservers;
    protected IConnectedClientsVisibility _visibility;

    void Establish()
    {
        _dormantObservers = Substitute.For<IDormantObservers>();
        _dormantObservers.Find(Arg.Any<ObserverKey>()).Returns(Task.FromResult<ObserverDurableSubscription?>(
            new ObserverDurableSubscription(ObserverSubscriptionKind.ClientReactor, false, null, [new("the-connection", "127.0.0.1:11111@1")])));
        _visibility = Substitute.For<IConnectedClientsVisibility>();
        _visibility.Filter(Arg.Any<IEnumerable<Contracts.Clients.ConnectedClient>>(), Arg.Any<ConnectionCaller>())
            .Returns(call => Task.FromResult(call.Arg<IEnumerable<Contracts.Clients.ConnectedClient>>()));
        _service = CreateService(_casing, _dormantObservers, _visibility);
    }
}
