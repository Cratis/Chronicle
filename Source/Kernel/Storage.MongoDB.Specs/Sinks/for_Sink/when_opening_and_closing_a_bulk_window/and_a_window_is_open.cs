// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_opening_and_closing_a_bulk_window;

public class and_a_window_is_open : given.a_sink_with_gated_bulk_writes
{
    SinkBufferingWindow _before;

    void Establish()
    {
        _holdFirstFlush = false;
        _before = _sink.BufferingWindow;
    }

    async Task Because() => await _sink.BeginBulk();

    [Fact] void should_be_open() => _sink.BufferingWindow.IsOpen.ShouldBeTrue();
    [Fact] void should_not_be_provably_closed_across_the_open() => SinkBufferingWindow.IsUnchangedAndClosed(_before, _sink.BufferingWindow).ShouldBeFalse();
}
