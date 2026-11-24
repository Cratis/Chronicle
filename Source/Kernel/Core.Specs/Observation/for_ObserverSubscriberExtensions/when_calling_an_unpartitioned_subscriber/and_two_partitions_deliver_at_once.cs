// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions.when_calling_an_unpartitioned_subscriber;

public class and_two_partitions_deliver_at_once : given.an_unpartitioned_subscriber
{
    int _inFlight;
    int _maximumInFlight;

    protected override async Task<ObserverSubscriberResult> HandleBatch(AppendedEvent[] batch)
    {
        var now = Interlocked.Increment(ref _inFlight);
        _maximumInFlight = Math.Max(_maximumInFlight, now);
        await Task.Delay(20);
        Interlocked.Decrement(ref _inFlight);
        return ObserverSubscriberResult.Ok(batch[^1].Context.SequenceNumber);
    }

    async Task Because() => await Task.WhenAll(
        _subscriber.OnNextWithin(TimeSpan.FromMinutes(1), "first", _events, new ObserverSubscriberContext(null)),
        _subscriber.OnNextWithin(TimeSpan.FromMinutes(1), "second", _events, new ObserverSubscriberContext(null)));

    [Fact] void should_never_run_two_calls_at_the_same_time() => _maximumInFlight.ShouldEqual(1);
}
