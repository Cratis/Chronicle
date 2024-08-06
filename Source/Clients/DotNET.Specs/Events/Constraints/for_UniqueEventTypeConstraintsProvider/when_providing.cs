// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintsProvider;

public class when_providing : Specification
{
    const string FirstEventMessage = "FirstEventMessage";
    const string SecondEventMessage = "SecondEventMessage";

    IClientArtifactsProvider _clientArtifactsProvider;
    IEventTypes _eventTypes;
    IImmutableList<IConstraintDefinition> _result;


    EventType _firstEventType;
    EventType _secondEventType;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _eventTypes = Substitute.For<IEventTypes>();

        _firstEventType = new EventType(nameof(FirstEvent), EventGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(FirstEvent)).Returns(_firstEventType);
        _secondEventType = new EventType(nameof(SecondEvent), EventGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(SecondEvent)).Returns(_secondEventType);

        _clientArtifactsProvider.UniqueEventTypeConstraints.Returns(
        [
            typeof(FirstEvent),
            typeof(SecondEvent)
        ]);
    }

    void Because() => _result = new UniqueEventTypeConstraintsProvider(_clientArtifactsProvider, _eventTypes).Provide();

    [Fact] void should_return_two_constraints() => _result.Count.ShouldEqual(2);
    [Fact] void should_return_constraint_for_first_event_type() => ((UniqueEventTypeConstraintDefinition)_result[0]).EventType.ShouldEqual(_firstEventType);
    [Fact] void should_return_constraint_for_first_event_type_as_name() => ((UniqueEventTypeConstraintDefinition)_result[0]).Name.ShouldEqual((ConstraintName)nameof(FirstEvent));
    [Fact] void should_return_constraint_for_first_event_type_message() => ((UniqueEventTypeConstraintDefinition)_result[0]).MessageCallback(null!).ShouldEqual((ConstraintViolationMessage)FirstEventMessage);

    [Fact] void should_return_constraint_for_second_event_type() => ((UniqueEventTypeConstraintDefinition)_result[1]).EventType.ShouldEqual(_secondEventType);
    [Fact] void should_return_constraint_for_second_event_type_as_name() => ((UniqueEventTypeConstraintDefinition)_result[1]).Name.ShouldEqual((ConstraintName)nameof(SecondEvent));
    [Fact] void should_return_constraint_for_second_event_type_message() => ((UniqueEventTypeConstraintDefinition)_result[1]).MessageCallback(null!).ShouldEqual((ConstraintViolationMessage)SecondEventMessage);

    [Unique(message: FirstEventMessage)] record FirstEvent;
    [Unique(message: SecondEventMessage)] record SecondEvent;
}
