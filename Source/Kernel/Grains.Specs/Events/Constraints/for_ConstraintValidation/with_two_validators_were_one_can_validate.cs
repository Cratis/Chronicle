// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_ConstraintValidation.when_establishing;

public class with_two_validators_were_one_can_validate : given.constraint_validation_with_two_validators
{
    EventSourceId _eventSourceId;
    EventType _eventType;
    ExpandoObject _content;
    ConstraintValidationContext _result;

    void Establish()
    {
        _firstValidator.CanValidate(Arg.Any<ConstraintValidationContext>()).Returns(false);
        _secondValidator.CanValidate(Arg.Any<ConstraintValidationContext>()).Returns(true);

        _eventSourceId = Guid.NewGuid();
        _eventType = new("SomeEvent", 1);
        _content = new ExpandoObject();
    }

    void Because() => _result = _constraintValidation.Establish(_eventSourceId, _eventType.Id, _content);

    [Fact] void should_have_event_source_id() => _result.EventSourceId.ShouldEqual(_eventSourceId);
    [Fact] void should_have_event_type() => _result.EventTypeId.ShouldEqual(_eventType.Id);
    [Fact] void should_have_content() => _result.Content.ShouldEqual(_content);
    [Fact] void should_have_both_validators() => _result.Validators.ShouldContainOnly([_secondValidator]);
}
