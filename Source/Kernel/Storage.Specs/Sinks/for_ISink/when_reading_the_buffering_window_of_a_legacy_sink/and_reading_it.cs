// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_reading_the_buffering_window_of_a_legacy_sink;

public class and_reading_it : Specification
{
    ISink _sink;
    SinkBufferingWindow _window;

    void Establish() => _sink = new a_legacy_sink();

    void Because() => _window = _sink.BufferingWindow;

    [Fact] void should_be_unknown() => _window.ShouldEqual(SinkBufferingWindow.Unknown);
    [Fact] void should_be_open() => _window.IsOpen.ShouldBeTrue();
    [Fact] void should_never_prove_persistence() => SinkBufferingWindow.IsUnchangedAndClosed(_window, _window).ShouldBeFalse();
}
