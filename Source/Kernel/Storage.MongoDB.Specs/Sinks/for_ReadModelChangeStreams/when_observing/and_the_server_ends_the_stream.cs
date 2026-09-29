// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_observing;

/// <summary>
/// The server ends a change stream only by invalidating it, which is not the end of the observation: the loop opens a
/// new stream and every subscriber reads again, since anything that happened while no stream was open is otherwise missed.
/// </summary>
public class and_the_server_ends_the_stream : given.a_shared_change_stream
{
    given.a_recording_observer<int> _observer;

    async Task Because()
    {
        _observer = new();
        using var subscription = Observe().Subscribe(_observer);
        await _observer.WaitForValues(1);

        Cursor(0).End();
        await _observer.WaitForValues(2);
    }

    [Fact] void should_reopen_the_change_stream() => Watches.ShouldEqual(2);
    [Fact] void should_read_again_when_it_is_reopened() => _observer.Values.Count.ShouldEqual(2);
    [Fact] void should_not_fail_the_observation() => _observer.HasFailed.ShouldBeFalse();
}
