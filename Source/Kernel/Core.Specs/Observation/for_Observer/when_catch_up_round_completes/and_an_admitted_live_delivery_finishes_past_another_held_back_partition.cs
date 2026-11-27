// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;
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
        var live = _observer.Handle("Q", [_liveEvent]);
        await _liveStarted.Task.WaitAsync(_deadline, TimeProvider.System);
        var acceptance = _observer.CaughtUp(JobId.NotSet, 13UL, 13UL);
        _liveResult.SetResult();
        await Task.WhenAll(live, acceptance).WaitAsync(_deadline, TimeProvider.System);
        await DrainRepairsAndRunWideReader();
    }

    [Fact] void should_freeze_after_the_settled_live_event() => _accepted.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)17UL);
    [Fact] void should_accept_both_prefix_owners_with_the_frozen_position() => _accepted.PendingCatchUpRepairs!.ShouldContainOnly(new PendingCatchUpRepair("P", 11UL, 17UL), new PendingCatchUpRepair("T", 15UL, 17UL));
    [Fact] void should_bound_both_repairs() => _acceptedRepairs.Select(_ => _.ToSequenceNumber).ShouldEqual([(EventSequenceNumber)16UL, (EventSequenceNumber)16UL]);
    [Fact] void should_start_the_wide_reader_after_the_live_event() => _wide!.FromEventSequenceNumber.ShouldEqual((EventSequenceNumber)17UL);
    [Fact] void should_deliver_each_event_once() => _delivered.OrderBy(_ => _).ShouldEqual([(EventSequenceNumber)11UL, (EventSequenceNumber)15UL, (EventSequenceNumber)16UL, (EventSequenceNumber)17UL]);
}
