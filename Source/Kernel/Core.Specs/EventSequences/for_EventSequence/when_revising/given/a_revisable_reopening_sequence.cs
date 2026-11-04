// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class a_revisable_reopening_sequence : a_revisable_sequence
{
    protected ClosedStreamScope _oldScope;
    protected ClosedStreamScope _newScope;
    protected virtual EventType Reopening => new("Reopened", EventTypeGeneration.First);
    protected virtual string RevisedScope => "second-stream";

    protected override EventSequenceNumber InitialNext => 3;
    protected override EventSequenceNumber? Watermark => 3;
    protected override bool Closing => true;
    protected override ClosedStreamDimensions ClosingDimensions => ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId;
    protected override string? ClosingEventStreamIdFrom => "email";

    async Task Establish()
    {
        await Seed(1, email: "second-stream");
        await Seed(2, type: Reopening);
        _oldScope = ClosedStreamScope.ForAppend(_eventSourceId, EventSourceType.Default, EventStreamType.All, "claimed@example.com", ClosingDimensions);
        _newScope = ClosedStreamScope.ForAppend(_eventSourceId, EventSourceType.Default, EventStreamType.All, "second-stream", ClosingDimensions);
        await _closures.Close(new(_oldScope, _closingDefinition.Name.Value, 0, DateTimeOffset.UtcNow));
        await _closures.Close(new(_newScope, _closingDefinition.Name.Value, 1, DateTimeOffset.UtcNow));
        if (_closingDefinition.ReopenedBy.Contains(Reopening.Id)) await _closures.Reopen(_closingDefinition.Name.Value, _oldScope);
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(Content(RevisedScope));
    }

    protected async Task ReviseAndRepair()
    {
        await ReviseAt(2, Reopening);
        await given.ConstraintRepairTimer.Tick(_silo);
    }
}
