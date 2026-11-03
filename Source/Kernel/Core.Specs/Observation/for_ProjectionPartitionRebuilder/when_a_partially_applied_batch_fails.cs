// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Observation.for_ProjectionPartitionRebuilder;

public class when_a_partially_applied_batch_fails : given.a_rebuilder
{
    int _mutations;
    bool _published;
    ObserverSubscriberResult _retry;

    async Task Establish()
    {
        _pipeline.Handle(Arg.Any<AppendedEvent>()).Returns(call =>
        {
            _mutations++;
            return Task.FromException<ProjectionEventContext>(new InvalidOperationException("Failed after mutating the accumulator"));
        });
        await _rebuilder.Begin(_observerKey, _partition);
    }

    async Task Because()
    {
        await _rebuilder.OnNext(_partition, [EventAt(1), EventAt(2)], new(null));
        _retry = await _rebuilder.OnNext(_partition, [EventAt(1), EventAt(2)], new(null));
        _published = await _rebuilder.Publish(2UL);
    }

    [Fact] void should_not_apply_the_partial_event_again() => _mutations.ShouldEqual(1);
    [Fact] void should_reject_redelivery_until_begin() => _retry.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_publish_the_poisoned_accumulator() => _published.ShouldBeFalse();
    [Fact] void should_never_touch_the_live_sink() => _liveSink.ReceivedCalls().ShouldBeEmpty();
}
