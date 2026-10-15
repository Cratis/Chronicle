// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_an_acknowledgment_skipped_another_index : given.an_event_sequence_with_repairable_indexes
{
    bool _otherWasIndexedAtPersistence;

    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
        _failWrites = false;
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(async call =>
        {
            await _log.AppendMany(call.Arg<IEnumerable<EventToAppendToStorage>>());
            return Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success([]);
        });
        var other = ValidatedEvent();
        other.Event = other.Event with { EventType = new("OtherEvent", EventTypeGeneration.First) };
        other.CompliantEvent = Content();
        await Cratis.Specifications.Catch.Exception(() => _eventSequence.AppendManyToStorage([other], CorrelationId.NotSet, [], []));
        _snapshots.BeforeWrite = async () => _otherWasIndexedAtPersistence = (await _claims.GetFor(other.Event.EventSourceId, _otherDefinition)).Any();
        _eventSourceId = EventSourceId.New();
    }

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_never_persist_a_watermark_ahead_of_the_other_index() => _otherWasIndexedAtPersistence.ShouldBeTrue();
    [Fact] async Task should_persist_the_completed_repair() => (await _log.GetState()).ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)2);
}
