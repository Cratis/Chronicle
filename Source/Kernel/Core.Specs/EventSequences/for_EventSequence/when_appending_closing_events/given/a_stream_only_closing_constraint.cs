// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_closing_events.given;

public class a_stream_only_closing_constraint : for_EventSequence.given.an_event_sequence
{
    protected ClosedStreamsConstraintStorage _closures;

    void Establish()
    {
        _closures = new();
        _namespaceStorage.GetClosedStreamsConstraints(SequenceId).Returns(_closures);
        var definition = new ClosesStreamConstraintDefinition("closing", [_eventType.Id], ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId, ["Reopened"]);
        _registeredConstraints.Add(definition);
        _currentValidation = new ConstraintValidation([new ClosesStreamConstraintValidator(definition, _closures), new ClosedStreamConstraintValidator(_closures, [definition.Dimensions], [definition])]);
    }
}
