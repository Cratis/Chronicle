// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidation.when_establishing;

public class with_event_metadata : given.constraint_validation_with_two_validators
{
    EventSourceId _eventSourceId;
    EventType _eventType;
    ExpandoObject _content;
    EventSourceType _eventSourceType;
    EventStreamType _eventStreamType;
    EventStreamId _eventStreamId;
    ConstraintValidationContext _result;

    void Establish()
    {
        _firstValidator.CanValidate(Arg.Any<ConstraintValidationContext>()).Returns(true);
        _secondValidator.CanValidate(Arg.Any<ConstraintValidationContext>()).Returns(true);

        _eventSourceId = Guid.NewGuid();
        _eventType = new("SomeEvent", 1);
        _content = new ExpandoObject();
        _eventSourceType = "MySourceType";
        _eventStreamType = "MyStreamType";
        _eventStreamId = "MyStreamId";
    }

    void Because() => _result = _constraintValidation.Establish(
        _eventSourceId, _eventType.Id, _content, _eventSourceType, _eventStreamType, _eventStreamId);

    [Fact] void should_have_event_source_id() => _result.EventSourceId.ShouldEqual(_eventSourceId);
    [Fact] void should_have_event_type() => _result.EventTypeId.ShouldEqual(_eventType.Id);
    [Fact] void should_have_content() => _result.Content.ShouldEqual(_content);
    [Fact] void should_have_event_source_type() => _result.EventSourceType.ShouldEqual(_eventSourceType);
    [Fact] void should_have_event_stream_type() => _result.EventStreamType.ShouldEqual(_eventStreamType);
    [Fact] void should_have_event_stream_id() => _result.EventStreamId.ShouldEqual(_eventStreamId);
    [Fact] void should_have_both_validators() => _result.Validators.ShouldContainOnly([_firstValidator, _secondValidator]);
}
