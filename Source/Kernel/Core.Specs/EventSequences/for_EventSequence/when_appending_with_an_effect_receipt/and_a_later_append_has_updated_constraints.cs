// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_a_later_append_has_updated_constraints : given.an_event_sequence_with_effect_receipts
{
    readonly Storage.InMemory.Events.Constraints.UniqueConstraintsStorage _index = new();
    UniqueConstraintDefinition _definition;
    UniqueConstraintValue _value;
    bool _released;

    async Task Establish()
    {
        var removal = new EventType("Released", EventTypeGeneration.First);
        _definition = new("unique-name", [new(_eventType.Id, ["name"])], [removal.Id]);
        _value = new[] { new UniqueConstraintPropertyAndValue("name", "one") }.GetValue(false);
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(_ =>
        {
            dynamic content = new ExpandoObject();
            content.name = "one";
            return (ExpandoObject)content;
        });
        _constraintValidation.Establish(Arg.Any<EventSourceId>(), Arg.Any<EventTypeId>(), Arg.Any<ExpandoObject>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<ConstraintBatchClaims?>(), Arg.Any<Subject?>(), Arg.Any<IReadOnlyCollection<string>?>())
            .Returns(call => new ConstraintValidationContext(
                [new UniqueConstraintValidator(_definition, _index), _recordingValidator],
                call.ArgAt<EventSourceId>(0),
                call.ArgAt<EventTypeId>(1),
                call.ArgAt<ExpandoObject>(2),
                call.ArgAt<EventSourceType?>(3),
                call.ArgAt<EventStreamType?>(4),
                call.ArgAt<EventStreamId?>(5),
                call.ArgAt<ConstraintBatchClaims?>(6)));

        await AppendEffect();
        (await _index.IsAllowed(new("another-source"), _definition, _value)).IsAllowed.ShouldBeFalse();
        var original = _receipt;
        _receipt = _receipt with { EffectKey = "release-name" };
        await AppendEffect([AnEvent with { EventType = removal }]);
        (await _index.IsAllowed(new("another-source"), _definition, _value)).IsAllowed.ShouldBeTrue();
        _receipt = original;
        _appendedEventsQueues.ClearReceivedCalls();
        _constraintValidation.ClearReceivedCalls();
    }

    async Task Because()
    {
        _result = await AppendEffect();
        _released = (await _index.IsAllowed(new("another-source"), _definition, _value)).IsAllowed;
    }

    [Fact] void should_return_the_recorded_outcome() => _result.EffectAlreadyRecorded.ShouldBeTrue();
    [Fact] void should_not_reserve_the_released_unique_value_again() => _released.ShouldBeTrue();
    [Fact] void should_not_roll_the_constraint_index_back() => _constraintIndexSequenceNumbers.ShouldEqual(new EventSequenceNumber[] { new(0), new(1) });
    [Fact] async Task should_not_dispatch_historical_events() => await _appendedEventsQueues.DidNotReceiveWithAnyArgs().Enqueue(default!);
    [Fact] void should_not_establish_historical_constraint_claims() => _constraintValidation.DidNotReceiveWithAnyArgs().Establish(default!, default!, default!, default, default, default, default);
    [Fact] void should_keep_the_next_slot() => _stateStorage.State.SequenceNumber.Value.ShouldEqual(2UL);
}
