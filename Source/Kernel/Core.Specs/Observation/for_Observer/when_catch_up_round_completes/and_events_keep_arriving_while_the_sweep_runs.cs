// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// Under a steady stream of appends the sweep always ends below the tail. The observer hands what it held back above
/// that to the subscriber and returns to live delivery, instead of starting sweep after sweep that each end the same way.
/// </summary>
public class and_events_keep_arriving_while_the_sweep_runs : given.an_observer_with_a_page_subscriber_catching_up
{
    static readonly Key _first = "first";
    static readonly Key _second = "second";
    List<(Key Partition, ulong SequenceNumber)> _delivered;

    void Establish()
    {
        _delivered = [];
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _delivered.AddRange(events.Select(_ => (call.Arg<Key>(), _.Context.SequenceNumber.Value)));
                return ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber.Value);
            });
    }

    async Task Because()
    {
        // Appended while the sweep runs: it reads up to 14 before it reports back.
        await _observer.Handle(_first, [EventAt(12)]);
        await _observer.Handle(_second, [EventAt(13)]);
        await _observer.Handle(_first, [EventAt(14)]);
        await _observer.Handle(_second, [EventAt(15)]);
        await _observer.Handle(_first, [EventAt(16)]);
        await _observer.Handle(_second, [EventAt(17)]);

        await _observer.CaughtUp(_job, 14UL, 14UL);

        // Still being appended after the sweep reported back.
        await _observer.Handle(_first, [EventAt(18)]);
        await _observer.Handle(_second, [EventAt(19)]);
    }

    [Fact] void should_deliver_nothing_the_sweep_read() => _delivered.Exists(_ => _.SequenceNumber <= 14UL).ShouldBeFalse();
    [Fact] void should_deliver_everything_above_what_the_sweep_read_exactly_once() => _delivered.Select(_ => _.SequenceNumber).Order().SequenceEqual([15UL, 16UL, 17UL, 18UL, 19UL]).ShouldBeTrue();
    [Fact] void should_keep_the_order_of_the_first_partition() => _delivered.Where(_ => _.Partition == _first).Select(_ => _.SequenceNumber).SequenceEqual([16UL, 18UL]).ShouldBeTrue();
    [Fact] void should_keep_the_order_of_the_second_partition() => _delivered.Where(_ => _.Partition == _second).Select(_ => _.SequenceNumber).SequenceEqual([15UL, 17UL, 19UL]).ShouldBeTrue();
    [Fact] void should_move_past_everything_delivered() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)20UL);
    [Fact] async Task should_be_back_on_live_delivery() => (await _observer.IsPreparingCatchup()).ShouldBeFalse();
    [Fact] async Task should_be_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_not_start_another_catch_up() => _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
