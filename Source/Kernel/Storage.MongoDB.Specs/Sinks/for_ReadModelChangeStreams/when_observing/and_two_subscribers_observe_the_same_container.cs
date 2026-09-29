// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_observing;

/// <summary>
/// Every observer of a collection shares one change stream, and each still reads for itself when it changes.
/// </summary>
public class and_two_subscribers_observe_the_same_container : given.a_shared_change_stream
{
    given.a_recording_observer<int> _first;
    given.a_recording_observer<int> _second;

    async Task Because()
    {
        _first = new();
        _second = new();
        using var first = Observe().Subscribe(_first);
        using var second = Observe().Subscribe(_second);
        await _first.WaitForValues(1);
        await _second.WaitForValues(1);

        Cursor(0).Report();
        await _first.WaitForValues(2);
        await _second.WaitForValues(2);
    }

    [Fact] void should_open_one_change_stream() => Watches.ShouldEqual(1);
    [Fact] void should_read_for_the_first_subscriber_when_opened_and_when_changed() => _first.Values.Count.ShouldEqual(2);
    [Fact] void should_read_for_the_second_subscriber_when_joining_and_when_changed() => _second.Values.Count.ShouldEqual(2);
}
