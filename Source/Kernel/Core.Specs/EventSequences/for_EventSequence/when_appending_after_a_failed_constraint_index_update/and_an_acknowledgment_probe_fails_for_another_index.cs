// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_an_acknowledgment_probe_fails_for_another_index : given.an_event_sequence_with_repairable_indexes
{
    bool _watermarkPassedOtherEvent;
    bool _everySnapshotKeptTheOtherClaim = true;
    EventSourceId _otherSource;

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
        _eventSequenceStorage.ReadBackAppend(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>()).ThrowsAsync(new given.SimulatedStorageError());
        _eventSequenceStorage.GetTailSequenceNumber().Returns(_ => _log.GetTailSequenceNumber());
        _eventSequenceStorage.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call => _log.GetRange(call.ArgAt<EventSequenceNumber>(0), call.ArgAt<EventSequenceNumber>(1), cancellationToken: call.ArgAt<CancellationToken>(5)));
        var other = ValidatedEvent();
        _otherSource = other.Event.EventSourceId;
        other.Event = other.Event with { EventType = new("OtherEvent", EventTypeGeneration.First) };
        other.CompliantEvent = Content();
        other.ConstraintContext = _currentValidation.Establish(_otherSource, other.Event.EventType.Id, other.CompliantEvent);
        var outcome = await _eventSequence.AppendManyToStorage([other], CorrelationId.NotSet, [], []);
        outcome.Errors.ShouldContain(AppendError.AppendOutcomeUnknown);
        _log.Events.Count.ShouldEqual(2);
        _snapshots.BeforeWrite = async () =>
        {
            if (_stateStorage.State.ConstraintIndexWatermark <= (EventSequenceNumber)1UL) return;
            _watermarkPassedOtherEvent = true;
            _everySnapshotKeptTheOtherClaim &= (await _claims.GetFor(_otherSource, _otherDefinition)).Any();
        };
        _eventSourceId = EventSourceId.New();
    }

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_repair_past_the_unknown_events_slot() => _watermarkPassedOtherEvent.ShouldBeTrue();
    [Fact] void should_never_persist_a_watermark_ahead_of_the_other_index() => _everySnapshotKeptTheOtherClaim.ShouldBeTrue();
    [Fact] async Task should_claim_the_other_events_value() => (await _claims.GetFor(_otherSource, _otherDefinition)).ShouldNotBeEmpty();
}
