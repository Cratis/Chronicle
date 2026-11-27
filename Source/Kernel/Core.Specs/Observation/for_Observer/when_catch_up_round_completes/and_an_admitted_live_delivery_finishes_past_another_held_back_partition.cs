// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_an_admitted_live_delivery_finishes_past_another_held_back_partition : given.an_observer_splitting_a_catch_up_prefix
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(5);
    readonly TaskCompletionSource _liveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _liveResult = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _subscriber.Configure().OnNext((Key)"Q", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(async call =>
    {
        _liveStarted.TrySetResult();
        await _liveResult.Task.WaitAsync(_deadline, TimeProvider.System);
        return Delivered(call.Arg<IEnumerable<AppendedEvent>>());
    });

    async Task Because()
    {
        await SetUpPrefix();
        var live = _observer.Handle("Q", [_liveEvent]);
        await _liveStarted.Task.WaitAsync(_deadline, TimeProvider.System);
        var acceptance = _observer.CaughtUp(JobId.NotSet, 13UL, 13UL);
        _liveResult.SetResult();
        await Task.WhenAll(live, acceptance).WaitAsync(_deadline, TimeProvider.System);
        await Join();
    }

    [Fact] void should_keep_the_unproved_prefix_restartable() => (_accepted <= 11UL).ShouldBeTrue();
    [Fact] void should_deliver_p() => _delivered.ShouldContain((EventSequenceNumber)11UL);
    [Fact] void should_deliver_t_if_applicable() => _delivered.Contains((EventSequenceNumber)15UL).ShouldEqual(HoldT);
    [Fact] void should_deliver_the_admitted_event() => _delivered.ShouldContain((EventSequenceNumber)16UL);
    [Fact] void should_deliver_the_later_event() => _delivered.ShouldContain((EventSequenceNumber)17UL);
}
