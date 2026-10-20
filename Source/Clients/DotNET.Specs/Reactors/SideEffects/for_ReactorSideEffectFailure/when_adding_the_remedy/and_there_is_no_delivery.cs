// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectFailure.when_adding_the_remedy;

public class and_there_is_no_delivery : Specification
{
    ReactorSideEffectFailure _failure;
    List<string> _messages;

    void Establish()
    {
        var delivery = new ReactorDelivery("remedy-none", "orders", "default", EventSequenceId.Log, EventSourceId.New(), 3UL);
        var conflict = new IdempotencyKeyConflict(delivery.ReturnedEffectKey(0), 1UL, new EventType("evt", EventTypeGeneration.First), EventTypeGeneration.First, IdempotencyConflictReason.Content);
        var result = AppendManyResult.Failed(CorrelationId.New(), [new AppendError("Refused")]) with { IdempotencyConflicts = [conflict] };
        _failure = ReactorSideEffectFailure.FromAppendResult(
            result,
            [],
            new ReactorContext(EventContext.Empty, new object(), ReactorContextValues.Empty));
    }

    void Because() => _messages = [.. _failure.GetMessages()];

    [Fact] void should_keep_the_kernels_sentence() => _messages.Exists(_ => _.Contains("Refused")).ShouldBeTrue();
    [Fact] void should_not_add_a_remedy() => _messages.Exists(_ => _.Contains("effectEpoch")).ShouldBeFalse();
}
