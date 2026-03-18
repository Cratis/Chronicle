// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintsProvider;

public class when_providing_with_removal_event : Specification
{
    const string EventMessage = "SomeMessage";

    IClientArtifactsProvider _clientArtifactsProvider;
    IEventTypes _eventTypes;
    EventType _constrainedEventType;
    EventType _removalEventType;
    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _eventTypes = Substitute.For<IEventTypes>();

        _constrainedEventType = new EventType(nameof(ConstrainedEvent), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(ConstrainedEvent)).Returns(_constrainedEventType);

        _removalEventType = new EventType(nameof(RemovalEvent), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(RemovalEvent)).Returns(_removalEventType);

        _clientArtifactsProvider.UniqueEventTypeConstraints.Returns([typeof(ConstrainedEvent)]);
        _clientArtifactsProvider.RemoveConstraintEventTypes.Returns([typeof(RemovalEvent)]);
    }

    void Because() => _result = new UniqueEventTypeConstraintsProvider(_clientArtifactsProvider, _eventTypes).Provide();

    [Fact] void should_return_one_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_set_removed_with_to_removal_event() => ((UniqueEventTypeConstraintDefinition)_result[0]).RemovedWith.ShouldEqual(_removalEventType.Id);

    [Unique(message: EventMessage)] record ConstrainedEvent;
    [RemoveConstraint(nameof(ConstrainedEvent))] record RemovalEvent;
}
