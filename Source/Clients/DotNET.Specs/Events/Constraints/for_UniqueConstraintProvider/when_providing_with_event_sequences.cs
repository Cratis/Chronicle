// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Several properties can share one constraint name, and each can declare the event sequences it applies to. They
/// are one constraint, so the declarations are combined rather than the first one winning.
/// </summary>
public class when_providing_with_event_sequences : Specification
{
    const string ConstraintName = "MyConstraint";
    const string CustomSequence = "custom-sequence";

    IClientArtifactsProvider _clientArtifactsProvider;
    IEventTypes _eventTypes;
    UniqueConstraintProvider _provider;
    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _eventTypes = Substitute.For<IEventTypes>();

        var eventType = new EventType(nameof(EventWithConstraint), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(EventWithConstraint)).Returns(eventType);
        _eventTypes.GetSchemaFor(eventType.Id).Returns(JsonSchema.FromType<EventWithConstraint>());

        var otherEventType = new EventType(nameof(OtherEventWithConstraint), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(OtherEventWithConstraint)).Returns(otherEventType);
        _eventTypes.GetSchemaFor(otherEventType.Id).Returns(JsonSchema.FromType<OtherEventWithConstraint>());

        _clientArtifactsProvider.UniqueConstraints.Returns([typeof(EventWithConstraint), typeof(OtherEventWithConstraint)]);
        _clientArtifactsProvider.RemoveConstraintEventTypes.Returns([]);

        _provider = new UniqueConstraintProvider(_clientArtifactsProvider, _eventTypes, new CamelCaseNamingPolicy());
    }

    void Because() => _result = _provider.Provide();

    [Fact] void should_return_one_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_apply_to_the_event_sequences_declared_on_every_property() => ((UniqueConstraintDefinition)_result[0]).EventSequences.ShouldContainOnly([EventSequenceId.Log, (EventSequenceId)CustomSequence]);

    record EventWithConstraint([property: Unique(ConstraintName, EventSequences = [EventSequenceId.LogId])] string Property);
    record OtherEventWithConstraint([property: Unique(ConstraintName, EventSequences = [CustomSequence])] string Property);
}
