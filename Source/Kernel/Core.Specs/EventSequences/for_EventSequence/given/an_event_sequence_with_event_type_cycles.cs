// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_event_type_cycles : appending_many_events
{
    protected static readonly EventType Covered = new("Covered", EventTypeGeneration.First);
    protected static readonly EventType CoveredRemoval = new("CoveredRemoval", EventTypeGeneration.First);
    protected static readonly EventType Removal = new("Removal", EventTypeGeneration.First);
    protected static readonly EventType OtherRemoval = new("OtherRemoval", EventTypeGeneration.First);

    protected List<UniqueEventTypeConstraintDefinition> _definitions;
    protected IUniqueEventTypesConstraintsStorage _uniqueEventTypesStorage;
    protected AppendManyResult _result;

    void Establish()
    {
        _definitions = [new("cycle", [Covered.Id, CoveredRemoval.Id], [CoveredRemoval.Id])];
        _uniqueEventTypesStorage = Substitute.For<IUniqueEventTypesConstraintsStorage>();
        _uniqueEventTypesStorage.IsAllowedWithinScope(Arg.Any<UniqueEventTypeConstraintDefinition>(), Arg.Any<EventSourceId>(), Arg.Any<ResolvedConstraintScope>())
            .Returns((true, EventSequenceNumber.Unavailable));

        // Keep the actual validation context and validators in the grain's append pipeline. Only the durable
        // history is substituted: it cannot see the events being validated until the whole batch is accepted.
        _constraintValidation.Establish(
            Arg.Any<EventSourceId>(),
            Arg.Any<EventTypeId>(),
            Arg.Any<ExpandoObject>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<ConstraintBatchClaims?>())
            .Returns(call => new ConstraintValidation(
                [.. _definitions.Select(definition => new UniqueEventTypeConstraintValidator(definition, _uniqueEventTypesStorage)), _recordingValidator])
                .Establish(
                    call.ArgAt<EventSourceId>(0),
                    call.ArgAt<EventTypeId>(1),
                    call.ArgAt<ExpandoObject>(2),
                    call.ArgAt<EventSourceType?>(3),
                    call.ArgAt<EventStreamType?>(4),
                    call.ArgAt<EventStreamId?>(5),
                    call.ArgAt<ConstraintBatchClaims?>(6)));

        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call => Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(
                AppendedEventsFrom(call.Arg<IEnumerable<EventToAppendToStorage>>()))));
    }

    protected EventToAppend EventFor(EventType eventType, EventSourceId? eventSourceId = default, EventStreamId? eventStreamId = default) =>
        EventToAppendFor(eventSourceId ?? _eventSourceId) with { EventType = eventType, eventStreamId = eventStreamId ?? EventStreamId.Default };
}
