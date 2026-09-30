// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_observing;

/// <summary>
/// A change stream that fails transiently is reopened, and the observer reads again rather than being ended.
/// </summary>
public class and_the_change_stream_fails_transiently : given.a_shared_change_stream
{
    given.a_recording_observer<int> _observer;

    async Task Because()
    {
        _observer = new();
        using var subscription = Observe().Subscribe(_observer);
        await _observer.WaitForValues(1);

        Cursor(0).Fail(new MongoClientException("The connection was lost"));
        await _observer.WaitForValues(2);
    }

    [Fact] void should_reopen_the_change_stream() => Watches.ShouldEqual(2);
    [Fact] void should_read_again_once_reopened() => _observer.Values.ShouldContainOnly(1, 2);
    [Fact] void should_not_end_the_observation() => _observer.HasFailed.ShouldBeFalse();
}
