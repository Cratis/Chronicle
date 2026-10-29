// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clustering;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors;

public class when_mixed_release_subscription_exhausts_retries : given.registrations
{
    readonly Exception _original = new TypeLoadException("first failure");
    Exception? _failure;
    int _calls;

    void Establish()
    {
        _releases.Summarize().Returns(new ClusterReleaseSummary([(new(19, 111, 0), 1), (new(19, 112, 0), 1)], 0, 0));
        _observer.Subscribe<IReactorObserverSubscriber<TenantReactor>>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), null, false)
            .Returns(_ => Task.FromException(++_calls == 1 ? _original : new InvalidOperationException("later failure")));
    }

    async Task Because() => _failure = await Catch.Exception(() => _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default));

    [Fact] void should_rethrow_the_first_failure() => _failure.ShouldEqual(_original);
    [Fact] void should_bound_subscriptions() => _calls.ShouldEqual(3);
    [Fact] void should_deactivate_only_before_retries() => _deactivated.Count.ShouldEqual(2);
}
