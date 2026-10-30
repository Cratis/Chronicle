// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_historical_catch_up_has_read_the_old_value : a_revisable_sequence
{
    int _writesBeforeRevision;
    int _readsBeforeRevision;

    protected override EventSequenceNumber InitialNext => 1001;
    protected override EventSequenceNumber? Watermark => null;
    protected override UniqueConstraintMode UniqueMode => UniqueConstraintMode.PerValue;

    async Task Establish()
    {
        for (ulong position = 1; position <= 1000; position++)
        {
            await Seed(position, type: new("OtherEvent", EventTypeGeneration.First));
        }
        await _claims.Save(_eventSourceId, _uniqueDefinition, 0, ValueOf("claimed@example.com"));
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(Content("revised@example.com"));

        // The historical reader retains the first 1000 events without writing an unfinished fold.
        await for_EventSequence.given.ConstraintRepairTimer.Tick(_silo);
        _writesBeforeRevision = _claimWrites;
        _readsBeforeRevision = _rangeReads;
    }

    async Task Because()
    {
        await ReviseFirstEvent();
        await for_EventSequence.given.ConstraintRepairTimer.Tick(_silo);
        await for_EventSequence.given.ConstraintRepairTimer.Tick(_silo);
        await for_EventSequence.given.ConstraintRepairTimer.Tick(_silo);
    }

    [Fact] void should_have_read_history_before_the_revision() => _readsBeforeRevision.ShouldBeGreaterThan(0);
    [Fact] void should_not_have_written_the_unfinished_historical_fold() => _writesBeforeRevision.ShouldEqual(0);
    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] async Task should_keep_the_revised_value_claimed() => (await _claims.IsAllowed("another-source", _uniqueDefinition, ValueOf("revised@example.com"))).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_not_restore_the_released_value() => (await _claims.IsAllowed("another-source", _uniqueDefinition, ValueOf("claimed@example.com"))).IsAllowed.ShouldBeTrue();
    [Fact] void should_finish_historical_catch_up() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
