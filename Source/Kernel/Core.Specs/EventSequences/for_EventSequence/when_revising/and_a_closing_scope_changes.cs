// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_a_closing_scope_changes : a_revisable_sequence
{
    ClosedStreamScope _oldScope;

    protected override EventSequenceNumber? Watermark => EventSequenceNumber.First;
    protected override bool Closing => true;
    protected override ClosedStreamDimensions ClosingDimensions => ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId;
    protected override string? ClosingEventStreamIdFrom => "email";

    async Task Establish()
    {
        _oldScope = ClosedStreamScope.ForAppend(_eventSourceId, EventSourceType.Default, EventStreamType.All, "old-stream", ClosingDimensions);
        await _closures.Close(new(_oldScope, _closingDefinition.Name.Value, 0, DateTimeOffset.UtcNow));
    }

    async Task Because() => await ReviseFirstEvent();

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] async Task should_reopen_the_closure_the_old_content_made() => (await _closures.GetFor(_closingDefinition.Name.Value, _oldScope)).ShouldBeNull();
    [Fact] void should_have_persisted_the_lowered_watermark_before_storage_revised() => _watermarkWhenStorageRevised.ShouldEqual(EventSequenceNumber.First);
}
