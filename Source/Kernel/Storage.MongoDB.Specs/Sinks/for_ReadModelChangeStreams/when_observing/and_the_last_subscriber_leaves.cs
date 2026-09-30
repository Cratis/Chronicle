// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_observing;

/// <summary>
/// The shared change stream is closed once no one observes the collection, and a later observer opens a new one.
/// </summary>
public class and_the_last_subscriber_leaves : given.a_shared_change_stream
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);

    bool _stoppedWatching;
    given.a_recording_observer<int> _later;

    async Task Because()
    {
        var first = new given.a_recording_observer<int>();
        var subscription = Observe().Subscribe(first);
        await first.WaitForValues(1);

        subscription.Dispose();
        await Cursor(0).Disposed.WaitAsync(_deadline);
        _stoppedWatching = WatchToken(0).IsCancellationRequested;

        _later = new();
        using var later = Observe().Subscribe(_later);
        await _later.WaitForValues(1);
    }

    [Fact] void should_stop_watching() => _stoppedWatching.ShouldBeTrue();
    [Fact] void should_open_a_new_change_stream_for_a_later_subscriber() => Watches.ShouldEqual(2);
    [Fact] void should_read_for_the_later_subscriber() => _later.Values.Count.ShouldEqual(1);
}
