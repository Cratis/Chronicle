// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using NJsonSchema;


namespace Cratis.Chronicle.Events.Constraints;

public class when_providing : Specification
{
    const string FirstConstraint = "FirstConstraint";
    const string SecondConstraint = "SecondConstraint";

    IClientArtifactsProvider _clientArtifactsProvider;
    IEventTypes _eventTypes;
    EventType _firstEventWithFirstConstraintEventType;
    UniqueConstraintProvider _provider;
    IImmutableList<IConstraintDefinition> _result;
    EventType _secondEventWithFirstConstraintEventType;
    EventType _firstEventWithSecondConstraintEventType;
    EventType _secondEventWithSecondConstraintEventType;
    JsonSchema _firstEventWithFirstConstraintSchema;
    JsonSchema _secondEventWithFirstConstraintSchema;
    JsonSchema _firstEventWithSecondConstraintSchema;
    JsonSchema _secondEventWithSecondConstraintSchema;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _eventTypes = Substitute.For<IEventTypes>();

        _firstEventWithFirstConstraintEventType = new EventType(nameof(FirstEventWithFirstConstraint), EventTypeGeneration.First);
        _firstEventWithFirstConstraintSchema = JsonSchema.FromType<FirstEventWithFirstConstraint>();
        _eventTypes.GetEventTypeFor(typeof(FirstEventWithFirstConstraint)).Returns(_firstEventWithFirstConstraintEventType);
        _eventTypes.GetSchemaFor(_firstEventWithFirstConstraintEventType.Id).Returns(_firstEventWithFirstConstraintSchema);

        _secondEventWithFirstConstraintEventType = new EventType(nameof(SecondEventWithFirstConstraint), EventTypeGeneration.First);
        _secondEventWithFirstConstraintSchema = JsonSchema.FromType<SecondEventWithFirstConstraint>();
        _eventTypes.GetEventTypeFor(typeof(SecondEventWithFirstConstraint)).Returns(_secondEventWithFirstConstraintEventType);
        _eventTypes.GetSchemaFor(_secondEventWithFirstConstraintEventType.Id).Returns(_secondEventWithFirstConstraintSchema);

        _firstEventWithSecondConstraintEventType = new EventType(nameof(FirstEventWithSecondConstraint), EventTypeGeneration.First);
        _firstEventWithSecondConstraintSchema = JsonSchema.FromType<FirstEventWithSecondConstraint>();
        _eventTypes.GetEventTypeFor(typeof(FirstEventWithSecondConstraint)).Returns(_firstEventWithSecondConstraintEventType);
        _eventTypes.GetSchemaFor(_firstEventWithSecondConstraintEventType.Id).Returns(_firstEventWithSecondConstraintSchema);

        _secondEventWithSecondConstraintEventType = new EventType(nameof(SecondEventWithSecondConstraint), EventTypeGeneration.First);
        _secondEventWithSecondConstraintSchema = JsonSchema.FromType<SecondEventWithSecondConstraint>();
        _eventTypes.GetEventTypeFor(typeof(SecondEventWithSecondConstraint)).Returns(_secondEventWithSecondConstraintEventType);
        _eventTypes.GetSchemaFor(_secondEventWithSecondConstraintEventType.Id).Returns(_secondEventWithSecondConstraintSchema);

        _clientArtifactsProvider.UniqueConstraints.Returns(
        [
            typeof(FirstEventWithFirstConstraint),
            typeof(SecondEventWithFirstConstraint),
            typeof(FirstEventWithSecondConstraint),
            typeof(SecondEventWithSecondConstraint)
        ]);

        _provider = new UniqueConstraintProvider(_clientArtifactsProvider, _eventTypes);
    }

    void Because() => _result = _provider.Provide();

    [Fact] void should_return_two_constraints() => _result.Count.ShouldEqual(2);
    [Fact] void should_hold_first_event_type_for_first_constraint() => ((UniqueConstraintDefinition)_result[0]).EventsWithProperties.First().EventTypeId.ShouldEqual(_firstEventWithFirstConstraintEventType.Id);
    [Fact] void should_hold_first_event_property_for_first_constraint() => ((UniqueConstraintDefinition)_result[0]).EventsWithProperties.First().Property.ShouldEqual(nameof(FirstEventWithFirstConstraint.firstEventWithFirstConstraintProperty));
    [Fact] void should_hold_first_event_schema_for_first_constraint() => ((UniqueConstraintDefinition)_result[0]).EventsWithProperties.First().Schema.ShouldEqual(_firstEventWithFirstConstraintSchema);
    [Fact] void should_hold_second_event_type_for_first_constraint() => ((UniqueConstraintDefinition)_result[0]).EventsWithProperties.Last().EventTypeId.ShouldEqual(_secondEventWithFirstConstraintEventType.Id);
    [Fact] void should_hold_second_event_property_for_first_constraint() => ((UniqueConstraintDefinition)_result[0]).EventsWithProperties.Last().Property.ShouldEqual(nameof(SecondEventWithFirstConstraint.secondEventWithFirstConstraintProperty));
    [Fact] void should_hold_second_event_schema_for_first_constraint() => ((UniqueConstraintDefinition)_result[0]).EventsWithProperties.Last().Schema.ShouldEqual(_secondEventWithFirstConstraintSchema);
    [Fact] void should_hold_first_event_type_for_second_constraint() => ((UniqueConstraintDefinition)_result[1]).EventsWithProperties.First().EventTypeId.ShouldEqual(_firstEventWithSecondConstraintEventType.Id);
    [Fact] void should_hold_first_event_property_for_second_constraint() => ((UniqueConstraintDefinition)_result[1]).EventsWithProperties.First().Property.ShouldEqual(nameof(FirstEventWithSecondConstraint.firstEventWithSecondConstraintProperty));
    [Fact] void should_hold_first_event_schema_for_second_constraint() => ((UniqueConstraintDefinition)_result[1]).EventsWithProperties.First().Schema.ShouldEqual(_firstEventWithSecondConstraintSchema);
    [Fact] void should_hold_second_event_type_for_second_constraint() => ((UniqueConstraintDefinition)_result[1]).EventsWithProperties.Last().EventTypeId.ShouldEqual(_secondEventWithSecondConstraintEventType.Id);
    [Fact] void should_hold_second_event_property_for_second_constraint() => ((UniqueConstraintDefinition)_result[1]).EventsWithProperties.Last().Property.ShouldEqual(nameof(SecondEventWithSecondConstraint.secondEventWithSecondConstraintProperty));
    [Fact] void should_hold_second_event_schema_for_second_constraint() => ((UniqueConstraintDefinition)_result[1]).EventsWithProperties.Last().Schema.ShouldEqual(_secondEventWithSecondConstraintSchema);

    record FirstEventWithFirstConstraint([property: Unique(FirstConstraint)] string firstEventWithFirstConstraintProperty);
    record SecondEventWithFirstConstraint([property: Unique(FirstConstraint)] string secondEventWithFirstConstraintProperty);
    record FirstEventWithSecondConstraint([property: Unique(SecondConstraint)] string firstEventWithSecondConstraintProperty);
    record SecondEventWithSecondConstraint([property: Unique(SecondConstraint)] string secondEventWithSecondConstraintProperty);
}
