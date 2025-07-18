// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

public class when_converting_to_client : Specification
{
    const string EventType = "Some event type";
    const ulong SequenceNumber = 42;
    const string ConstraintName = "Some constraint name";
    const string Message = "Some message";
    const string FirstDetailsProperty = "First details property";
    const string FirstDetailsValue = "First details value";
    const string SecondDetailsProperty = "Second details property";
    const string SecondDetailsValue = "Second details value";

    ConstraintViolation _result;

    void Because() => _result = new Contracts.Events.Constraints.ConstraintViolation
    {
        EventTypeId = EventType,
        SequenceNumber = SequenceNumber,
        ConstraintName = ConstraintName,
        Message = Message,
        Details = new Dictionary<string, string>
        {
            { FirstDetailsProperty, FirstDetailsValue },
            { SecondDetailsProperty, SecondDetailsValue }
        }
    }.ToClient();

    [Fact] void should_convert_event_type() => _result.EventTypeId.Value.ShouldEqual(EventType);
    [Fact] void should_convert_sequence_number() => _result.SequenceNumber.Value.ShouldEqual(SequenceNumber);
    [Fact] void should_convert_constraint_name() => _result.ConstraintName.Value.ShouldEqual(ConstraintName);
    [Fact] void should_convert_message() => _result.Message.Value.ShouldEqual(Message);
    [Fact]
    void should_convert_details() => _result.Details.ShouldContainOnly(
        new KeyValuePair<string, string>(FirstDetailsProperty, FirstDetailsValue),
        new KeyValuePair<string, string>(SecondDetailsProperty, SecondDetailsValue));
}
