// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_state_concurrently;

public class and_a_callback_relinquishes_while_a_subscribe_reads_the_definition : given.a_live_observer_with_overlap_guarded_definition
{
    Exception? _callbackError;
    Exception? _subscribeError;

    /// <summary>
    /// Orleans runs one turn of a grain at a time and interleaves only at awaits; the calls share this exclusive
    /// scheduler so they interleave the same way, instead of running on two threads at once.
    /// </summary>
    readonly TaskScheduler _grainScheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;

    async Task Because()
    {
        _guardedDefinition.HoldNextRead = true;
        var subscribing = OnGrainScheduler(() => _observer.SubscribeToAllEvents<NullObserverSubscriber>(ObserverType.Projection, SiloAddress.Zero));
        await WaitFor(_guardedDefinition.ReadStarted);
        await MoveDefinition();

        var callback = OnGrainScheduler(() => _observer.SetHandledStats(99));
        _guardedDefinition.ReleaseRead.TrySetResult();
        _subscribeError = await Capture(subscribing);
        _callbackError = await Capture(callback);
    }

    Task OnGrainScheduler(Func<Task> call) => Task.Factory.StartNew(call, CancellationToken.None, TaskCreationOptions.None, _grainScheduler).Unwrap();

    static async Task<Exception?> Capture(Task task)
    {
        try
        {
            await task;
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Fact] void should_not_fail_the_callback() => _callbackError.ShouldBeNull();
    [Fact] void should_not_fail_the_subscribe() => _subscribeError.ShouldBeNull();
    [Fact] void should_never_read_the_definition_twice_at_once() => _guardedDefinition.MaxReadsInFlight.ShouldEqual(1);
}
