// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_opening_and_closing_a_bulk_window;

public class and_no_window_was_opened : given.a_sink_with_gated_bulk_writes
{
    SinkBufferingWindow _before;
    SinkBufferingWindow _after;

    void Establish() => _holdFirstFlush = false;

    async Task Because()
    {
        _before = _sink.BufferingWindow;
        await Apply(_firstKey, 1, 12);
        _after = _sink.BufferingWindow;
    }

    [Fact] void should_be_closed_and_unchanged() => SinkBufferingWindow.IsUnchangedAndClosed(_before, _after).ShouldBeTrue();
}
