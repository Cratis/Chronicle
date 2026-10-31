// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_opening_and_closing_a_bulk_window;

public class and_the_window_was_ended : given.a_sink_with_gated_bulk_writes
{
    SinkBufferingWindow _before;

    async Task Establish()
    {
        _holdFirstFlush = false;
        _before = _sink.BufferingWindow;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 12);
    }

    async Task Because() => await _sink.EndBulk();

    [Fact] void should_be_closed() => _sink.BufferingWindow.IsOpen.ShouldBeFalse();
    [Fact] void should_have_a_greater_stamp() => _sink.BufferingWindow.Value.ShouldBeGreaterThan(_before.Value);
    [Fact] void should_not_vouch_for_a_delivery_that_spanned_the_window() => SinkBufferingWindow.IsUnchangedAndClosed(_before, _sink.BufferingWindow).ShouldBeFalse();
}
