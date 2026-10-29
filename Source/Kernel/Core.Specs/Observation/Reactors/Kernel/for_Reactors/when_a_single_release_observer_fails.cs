// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors;

public class when_a_single_release_observer_fails : given.registrations
{
    readonly Exception _original = new TypeLoadException("single release");
    Exception? _failure;
    int _calls;

    void Establish() => _observer.Subscribe<IReactorObserverSubscriber<TenantReactor>>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), null, false)
        .Returns(_ =>
        {
            _calls++;
            return Task.FromException(_original);
        });

    async Task Because() => _failure = await Catch.Exception(() => _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default));

    [Fact] void should_propagate_the_failure() => _failure.ShouldEqual(_original);
    [Fact] void should_not_retry() => _calls.ShouldEqual(1);
    [Fact] void should_not_deactivate() => _deactivated.ShouldBeEmpty();
}
