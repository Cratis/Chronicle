// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectFailure.when_adding_the_remedy;

public class and_the_reactor_has_an_explicit_event_sequence : Specification
{
    ReactorSideEffectFailure _failure;
    List<string> _messages;

    void Establish()
    {
        var delivery = new ReactorDelivery("remedy-explicit", "orders", "default", EventSequenceId.Log, EventSourceId.New(), 3UL);
        var conflict = new IdempotencyKeyConflict(delivery.ReturnedEffectKey(0), 1UL, new EventType("evt", EventTypeGeneration.First), EventTypeGeneration.First, IdempotencyConflictReason.Content);
        var result = AppendManyResult.Failed(CorrelationId.New(), [new AppendError("Refused")]) with { IdempotencyConflicts = [conflict] };
        _failure = ReactorSideEffectFailure.FromAppendResult(
            result,
            [],
            new ReactorContext(EventContext.Empty, new Reactor(), ReactorContextValues.Empty) { Delivery = delivery });
    }

    void Because() => _messages = [.. _failure.GetMessages()];

    [Fact] void should_keep_the_kernels_sentence() => _messages.Exists(_ => _.Contains("Refused")).ShouldBeTrue();
    [Fact] void should_name_the_reactor_and_the_next_epoch() => _messages.Exists(_ => _.Contains("effectEpoch: 1)]")).ShouldBeTrue();
    [Fact] void should_name_the_reactor_id() => _messages.Exists(_ => _.Contains("Reactor 'remedy-explicit' produced different output")).ShouldBeTrue();
    [Fact] void should_point_to_once_only() => _messages.Exists(_ => _.Contains("[OnceOnly]")).ShouldBeTrue();
    [Fact] void should_name_the_event_sequence() => _messages.Exists(_ => _.Contains("\"inbox\"")).ShouldBeTrue();

    [Reactor("remedy-explicit", "inbox")]
    class Reactor;
}
