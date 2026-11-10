// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// A sweep that read up to 14 while live delivery held back 12 to 17, whose report hands 15, 16 and 17 over. The
/// subscriber does not finish 15 until <see cref="ReleaseTheFirstHandedOverEvent"/>, so the handover is caught
/// mid-way, at an await where other calls interleave with it.
/// </summary>
public class an_observer_handing_over_what_a_sweep_held_back : an_observer_with_a_page_subscriber_catching_up
{
    protected static readonly Key First = "first";
    protected static readonly Key Second = "second";
    protected List<ulong> _delivered;
    TaskCompletionSource<ObserverSubscriberResult> _firstHandedOver;

    async Task Establish()
    {
        _delivered = [];
        _firstHandedOver = new();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _delivered.AddRange(events.Select(_ => _.Context.SequenceNumber.Value));
                return events[0].Context.SequenceNumber.Value == 15UL
                    ? _firstHandedOver.Task
                    : Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber.Value));
            });

        await _observer.Handle(First, [EventAt(12)]);
        await _observer.Handle(Second, [EventAt(13)]);
        await _observer.Handle(First, [EventAt(14)]);
        await _observer.Handle(Second, [EventAt(15)]);
        await _observer.Handle(First, [EventAt(16)]);
        await _observer.Handle(Second, [EventAt(17)]);
    }

    protected Task StartHandover() => _observer.CaughtUp(_job, 14UL, 14UL);

    protected void ReleaseTheFirstHandedOverEvent() => _firstHandedOver.SetResult(ObserverSubscriberResult.Ok(15UL));
}
