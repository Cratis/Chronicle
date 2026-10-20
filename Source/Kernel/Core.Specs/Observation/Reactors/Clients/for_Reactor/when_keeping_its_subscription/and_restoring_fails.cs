// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class and_restoring_fails : given.a_subscribed_reactor_grain
{
    void Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(false);
        _observer.RestoreClientSubscription<IReactorObserverSubscriber>(
            Arg.Any<ObserverType>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Any<ConnectedClient>(),
            Arg.Any<bool>(),
            Arg.Any<ObserverFilters?>(),
            Arg.Any<EventGenerationDelivery>()).Returns(Task.FromException(new InvalidOperationException("The silo is leaving")));
    }

    async Task Because()
    {
        await _silo.FireAllTimersAsync();
        await _silo.FireAllTimersAsync();
    }

    [Fact] Task should_retry_at_the_next_check() => WasRestored(2);
}
