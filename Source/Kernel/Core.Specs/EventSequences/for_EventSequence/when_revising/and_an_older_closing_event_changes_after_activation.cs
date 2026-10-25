// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_an_older_closing_event_changes_after_activation : a_revisable_sequence
{
    ClosedStreamScope _oldScope;
    ClosedStreamScope _newScope;

    protected override EventSequenceNumber InitialNext => 3;
    protected override EventSequenceNumber? Watermark => 3;
    protected override bool Closing => true;
    protected override ClosedStreamDimensions ClosingDimensions => ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId;
    protected override string? ClosingEventStreamIdFrom => "email";

    async Task Establish()
    {
        await Seed(1, type: new("Unrelated", EventTypeGeneration.First));
        await Seed(2, type: new("Unrelated", EventTypeGeneration.First));
        _oldScope = ClosedStreamScope.ForAppend(_eventSourceId, EventSourceType.Default, EventStreamType.All, "claimed@example.com", ClosingDimensions);
        _newScope = ClosedStreamScope.ForAppend(_eventSourceId, EventSourceType.Default, EventStreamType.All, "new-stream", ClosingDimensions);
        await _closures.Close(new(_oldScope, _closingDefinition.Name.Value, 0, DateTimeOffset.UtcNow));
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(Content("new-stream"));
    }

    async Task Because()
    {
        await ReviseFirstEvent();
        await for_EventSequence.given.ConstraintRepairTimer.Tick(_silo);
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_persist_coverage_from_the_revised_event_before_storage_revises() => _watermarkWhenStorageRevised.ShouldEqual(EventSequenceNumber.First);
    [Fact] async Task should_leave_the_old_scope_open_after_refolding() => (await _closures.GetFor(_closingDefinition.Name.Value, _oldScope)).ShouldBeNull();
    [Fact] async Task should_close_the_new_scope_after_refolding() => (await _closures.GetFor(_closingDefinition.Name.Value, _newScope))!.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_finish_the_repair() => _snapshots.Written!.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)3);
}
