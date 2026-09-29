// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintsProvider;

public class when_providing_with_event_sequences : Specification
{
    IClientArtifactsProvider _clientArtifactsProvider;
    IEventTypes _eventTypes;
    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.GetEventTypeFor(typeof(ConstrainedEvent)).Returns(new EventType(nameof(ConstrainedEvent), EventTypeGeneration.First));

        _clientArtifactsProvider.UniqueEventTypeConstraints.Returns([typeof(ConstrainedEvent)]);
        _clientArtifactsProvider.RemoveConstraintEventTypes.Returns([]);
    }

    void Because() => _result = new UniqueEventTypeConstraintsProvider(_clientArtifactsProvider, _eventTypes).Provide();

    [Fact] void should_apply_to_the_declared_event_sequences_only() => ((UniqueEventTypeConstraintDefinition)_result[0]).EventSequences.ShouldContainOnly([EventSequenceId.Log]);

    [Unique(EventSequences = [EventSequenceId.LogId])] record ConstrainedEvent;
}
