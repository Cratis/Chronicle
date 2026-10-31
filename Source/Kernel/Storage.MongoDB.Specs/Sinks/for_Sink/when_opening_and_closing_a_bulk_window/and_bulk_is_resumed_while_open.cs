// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_opening_and_closing_a_bulk_window;

public class and_bulk_is_resumed_while_open : given.a_sink_with_gated_bulk_writes
{
    SinkBufferingWindow _open;

    async Task Establish()
    {
        _holdFirstFlush = false;
        await _sink.BeginBulk();
        _open = _sink.BufferingWindow;
    }

    async Task Because() => await _sink.ResumeBulk();

    [Fact] void should_not_change_the_stamp() => _sink.BufferingWindow.ShouldEqual(_open);
    [Fact] void should_still_be_open() => _sink.BufferingWindow.IsOpen.ShouldBeTrue();
}
