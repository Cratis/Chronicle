// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors;

public class when_subscription_succeeds_in_a_mixed_cluster : given.registrations
{
    void Establish() => _releases.Summarize().Returns(new ClusterReleaseSummary([(new(19, 111, 0), 1), (new(19, 112, 0), 1)], 0, 0));
    Task Because() => _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default);
    [Fact] void should_not_deactivate_the_observer() => _deactivated.ShouldBeEmpty();
    [Fact] async Task should_subscribe_only_once() => await _observer.Received(1).Subscribe<IReactorObserverSubscriber<TenantReactor>>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), null, false);
}
