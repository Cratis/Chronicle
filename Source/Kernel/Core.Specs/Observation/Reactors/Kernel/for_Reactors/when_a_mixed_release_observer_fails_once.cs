// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors;

public class when_a_mixed_release_observer_fails_once : given.registrations
{
    int _calls;
    Exception? _failure;

    void Establish()
    {
        _releases.Summarize().Returns(new ClusterReleaseSummary([(new(19, 111, 0), 1), (new(19, 112, 0), 1)], 0, 0));
        _observer.Subscribe<IReactorObserverSubscriber<TenantReactor>>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), null, false)
            .Returns(_ => ++_calls == 1 ? Task.FromException(new TypeLoadException("old observer")) : Task.CompletedTask);
    }

    async Task Because() => _failure = await Catch.Exception(() => _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default));

    [Fact] void should_succeed_on_the_next_activation() => _failure.ShouldBeNull();
    [Fact] void should_deactivate_the_old_observer_once() => _deactivated.ShouldContainOnly(_observer);
    [Fact] void should_log_a_retry_warning() => _logger.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == "Log").ShouldEqual(1);
    [Fact] void should_subscribe_again() => _calls.ShouldEqual(2);
}
