// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintsProvider;

/// <summary>
/// The attribute form of the unique-event-type release. Several event types may carry <c>[RemoveConstraint]</c> for
/// one constraint name, and each of them ends a cycle. The provider used to take the first one it found, so the rest
/// declared a release that never happened.
/// </summary>
public class when_providing_with_several_removal_events : Specification
{
    IClientArtifactsProvider _clientArtifactsProvider;
    IEventTypes _eventTypes;
    EventType _constrainedEventType;
    EventType _firstRemovalEventType;
    EventType _secondRemovalEventType;
    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _eventTypes = Substitute.For<IEventTypes>();

        _constrainedEventType = new EventType(nameof(ConstrainedEvent), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(ConstrainedEvent)).Returns(_constrainedEventType);

        _firstRemovalEventType = new EventType(nameof(FirstRemovalEvent), EventTypeGeneration.First);
        _secondRemovalEventType = new EventType(nameof(SecondRemovalEvent), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(FirstRemovalEvent)).Returns(_firstRemovalEventType);
        _eventTypes.GetEventTypeFor(typeof(SecondRemovalEvent)).Returns(_secondRemovalEventType);

        _clientArtifactsProvider.UniqueEventTypeConstraints.Returns([typeof(ConstrainedEvent)]);
        _clientArtifactsProvider.RemoveConstraintEventTypes.Returns([typeof(FirstRemovalEvent), typeof(SecondRemovalEvent)]);
    }

    void Because() => _result = new UniqueEventTypeConstraintsProvider(_clientArtifactsProvider, _eventTypes).Provide();

    [Fact] void should_return_one_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_carry_every_event_declaring_the_removal() => ((UniqueEventTypeConstraintDefinition)_result[0]).RemovedWith.ShouldContainOnly([_firstRemovalEventType.Id, _secondRemovalEventType.Id]);

    [Unique] record ConstrainedEvent;
    [RemoveConstraint(nameof(ConstrainedEvent))] record FirstRemovalEvent;
    [RemoveConstraint(nameof(ConstrainedEvent))] record SecondRemovalEvent;
}
