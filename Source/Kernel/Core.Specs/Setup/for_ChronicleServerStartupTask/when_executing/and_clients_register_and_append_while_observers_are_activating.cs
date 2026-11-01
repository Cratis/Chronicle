// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Clients can register observers and append events before the background pass reaches an observer. That is safe
/// because the pass only ever asks the observer grain to ensure itself, which is idempotent and serialized by the
/// grain with the client's own calls. It never subscribes, replays or appends, and never overlaps itself for a key.
/// </summary>
public class and_clients_register_and_append_while_observers_are_activating : given.a_startup_task_with_a_persisted_reactor
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _inFlight;
    int _maxInFlight;
    int _calls;

    void Establish() => _reactorObserver.Ensure().Returns(async _ =>
    {
        var current = Interlocked.Increment(ref _inFlight);
        _maxInFlight = Math.Max(_maxInFlight, current);
        try
        {
            if (Interlocked.Increment(ref _calls) <= 2)
            {
                throw new TimeoutException();
            }

            await _release.Task;
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    });

    async Task Because()
    {
        await Execute();

        // While the pass is still retrying, the grain is free to serve the client's registration and appends.
        await Task.Delay(100);
        _release.SetResult();
        await RecoverableWork.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_never_overlap_activations_of_the_observer() => _maxInFlight.ShouldEqual(1);
    [Fact] void should_only_ask_the_observer_to_ensure_itself() => _reactorObserver.ReceivedCalls().Select(_ => _.GetMethodInfo().Name).Distinct().ShouldContainOnly([nameof(Cratis.Chronicle.Observation.IObserver.Ensure)]);
    [Fact] void should_only_rehydrate_the_event_sequences() => _eventSequences.ReceivedCalls().Select(_ => _.GetMethodInfo().Name).Distinct().ShouldContainOnly([nameof(Cratis.Chronicle.EventSequences.IEventSequences.Rehydrate)]);
}
