// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintProvider;

/// <summary>
/// The attribute form of the same rule: <c>[RemoveConstraint]</c> may be written on as many event types as the
/// lifecycle has terminal facts, and each of them releases the constraint. The provider used to take the first one
/// it found, so every other event carrying the attribute for that name declared a release that never happened.
/// </summary>
public class when_providing_with_several_removal_events : Specification
{
    const string ConstraintName = "MyConstraint";

    IClientArtifactsProvider _clientArtifactsProvider;
    IEventTypes _eventTypes;
    EventType _eventType;
    EventType _firstRemovalEventType;
    EventType _secondRemovalEventType;
    UniqueConstraintProvider _provider;
    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _eventTypes = Substitute.For<IEventTypes>();

        _eventType = new EventType(nameof(EventWithConstraint), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(EventWithConstraint)).Returns(_eventType);
        _eventTypes.GetSchemaFor(_eventType.Id).Returns(JsonSchema.FromType<EventWithConstraint>());

        _firstRemovalEventType = new EventType(nameof(FirstRemovalEvent), EventTypeGeneration.First);
        _secondRemovalEventType = new EventType(nameof(SecondRemovalEvent), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(FirstRemovalEvent)).Returns(_firstRemovalEventType);
        _eventTypes.GetEventTypeFor(typeof(SecondRemovalEvent)).Returns(_secondRemovalEventType);

        _clientArtifactsProvider.UniqueConstraints.Returns([typeof(EventWithConstraint)]);
        _clientArtifactsProvider.RemoveConstraintEventTypes.Returns([typeof(FirstRemovalEvent), typeof(SecondRemovalEvent)]);

        _provider = new UniqueConstraintProvider(_clientArtifactsProvider, _eventTypes, new CamelCaseNamingPolicy());
    }

    void Because() => _result = _provider.Provide();

    [Fact] void should_return_one_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_carry_every_event_declaring_the_removal() => ((UniqueConstraintDefinition)_result[0]).RemovedWith.ShouldContainOnly([_firstRemovalEventType.Id, _secondRemovalEventType.Id]);

    record EventWithConstraint([property: Unique(ConstraintName)] string Property);
    [RemoveConstraint(ConstraintName)] record FirstRemovalEvent;
    [RemoveConstraint(ConstraintName)] record SecondRemovalEvent;
}
