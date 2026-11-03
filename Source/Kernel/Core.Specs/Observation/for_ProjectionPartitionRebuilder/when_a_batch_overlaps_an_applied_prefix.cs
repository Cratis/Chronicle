// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ProjectionPartitionRebuilder;

public class when_a_batch_overlaps_an_applied_prefix : given.a_rebuilder
{
    ObserverSubscriberResult _result;

    async Task Establish()
    {
        await _rebuilder.Begin(_observerKey, _partition);
        await _rebuilder.OnNext(_partition, [EventAt(1), EventAt(2)], new(null));
    }

    async Task Because() => _result = await _rebuilder.OnNext(_partition, [EventAt(1), EventAt(2), EventAt(3)], new(null));

    [Fact] void should_apply_the_new_suffix() => _pipeline.Received(1).Handle(Arg.Is<AppendedEvent>(_ => _.Context.SequenceNumber == (EventSequenceNumber)3UL));
    [Fact] void should_not_apply_the_prefix_again() => _pipeline.Received(1).Handle(Arg.Is<AppendedEvent>(_ => _.Context.SequenceNumber == (EventSequenceNumber)1UL));
    [Fact] void should_not_apply_the_prefix_tail_again() => _pipeline.Received(1).Handle(Arg.Is<AppendedEvent>(_ => _.Context.SequenceNumber == (EventSequenceNumber)2UL));
    [Fact] void should_acknowledge_the_full_redelivery() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)3UL);
}
