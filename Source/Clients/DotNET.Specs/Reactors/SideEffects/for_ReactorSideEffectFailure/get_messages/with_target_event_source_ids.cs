// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectFailure.get_messages;

public class with_target_event_source_ids : Specification
{
    readonly EventSourceId _firstId = EventSourceId.New();
    readonly EventSourceId _secondId = EventSourceId.New();
    string[] _result;

    void Because()
    {
        var failure = new ReactorSideEffectFailure(
            [new AppendFailure([new ReactorConstraintViolation("SomeEvent", "must be unique")], false, [], [_firstId, _secondId])]);

        _result = failure.GetMessages().ToArray();
    }

    [Fact] void should_have_the_target_ids_message_and_the_constraint_message() => _result.Length.ShouldEqual(2);
    [Fact] void should_list_all_target_event_source_ids() => _result[0].ShouldEqual($"Append failure 1: Failed appending to event source id(s) '{_firstId}', '{_secondId}'");
    [Fact] void should_still_include_the_constraint_violation() => _result[1].ShouldEqual("Append failure 1: Constraint violation for event type 'SomeEvent': must be unique");
}
