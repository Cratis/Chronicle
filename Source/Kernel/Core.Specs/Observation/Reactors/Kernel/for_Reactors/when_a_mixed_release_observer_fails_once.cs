// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors;

public class when_a_mixed_release_observer_fails_once : given.registrations
{
    readonly TaskCompletionSource _retryEnsureEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _retryEnsureCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _calls;
    int _ensureCalls;
    int _subscriptionsBeforeRetryEnsureCompleted;
    Exception? _failure;

    void Establish()
    {
        _releases.Summarize().Returns(new ClusterReleaseSummary([(new(19, 111, 0), 1), (new(19, 112, 0), 1)], 0, 0));
        _observer.Ensure().Returns(_ =>
        {
            if (++_ensureCalls == 2)
            {
                _retryEnsureEntered.SetResult();
                return _retryEnsureCompleted.Task;
            }
            return Task.CompletedTask;
        });
        _observer.Subscribe<IReactorObserverSubscriber<TenantReactor>>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), null, false)
            .Returns(_ => ++_calls == 1 ? Task.FromException(new TypeLoadException("old observer")) : Task.CompletedTask);
    }

    async Task Because() => _failure = await Catch.Exception(async () =>
    {
        var registration = _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default);
        try
        {
            await _retryEnsureEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            _subscriptionsBeforeRetryEnsureCompleted = _calls;
        }
        finally
        {
            _retryEnsureCompleted.TrySetResult();
        }
        await registration.WaitAsync(TimeSpan.FromSeconds(5));
    });

    [Fact] void should_succeed_on_the_next_activation() => _failure.ShouldBeNull();
    [Fact] void should_ensure_again() => _ensureCalls.ShouldEqual(2);
    [Fact] void should_await_ensure_before_retrying() => _subscriptionsBeforeRetryEnsureCompleted.ShouldEqual(1);
    [Fact] void should_log_a_retry_warning() => _logger.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == "Log").ShouldEqual(1);
    [Fact] void should_subscribe_again() => _calls.ShouldEqual(2);
}
