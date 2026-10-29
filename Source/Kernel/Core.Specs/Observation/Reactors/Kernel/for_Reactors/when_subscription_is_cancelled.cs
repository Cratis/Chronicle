// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors;

public class when_subscription_is_cancelled : given.registrations
{
    readonly CancellationTokenSource _cancellation = new();
    readonly Exception _original = new OperationCanceledException();
    Exception? _failure;
    int _calls;

    void Establish()
    {
        _releases.Summarize().Returns(new ClusterReleaseSummary([(new(19, 111, 0), 1), (new(19, 112, 0), 1)], 0, 0));
        _observer.Subscribe<IReactorObserverSubscriber<TenantReactor>>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), null, false)
            .Returns(async _ =>
            {
                _calls++;
                await _cancellation.CancelAsync();
                throw _original;
            });
    }
    async Task Because() => _failure = await Catch.Exception(() => _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default, _cancellation.Token));
    void Destroy() => _cancellation.Dispose();
    [Fact] void should_preserve_cancellation() => _failure.ShouldEqual(_original);
    [Fact] void should_not_retry() => _calls.ShouldEqual(1);
    [Fact] void should_not_deactivate() => _deactivated.ShouldBeEmpty();
}
