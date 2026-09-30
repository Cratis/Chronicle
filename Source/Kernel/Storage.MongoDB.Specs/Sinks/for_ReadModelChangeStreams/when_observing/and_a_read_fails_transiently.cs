// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_observing;

/// <summary>
/// A read cut short by a transient failure - a find killed by a concurrent rename, say - is retried.
/// </summary>
public class and_a_read_fails_transiently : given.a_shared_change_stream
{
    given.a_recording_observer<int> _observer;

    void Establish() =>
        _read = attempt => attempt == 1 ? Task.FromException<int>(new MongoClientException("The query was killed")) : Task.FromResult(attempt);

    async Task Because()
    {
        _observer = new();
        using var subscription = Observe().Subscribe(_observer);
        await _observer.WaitForValues(1);
    }

    [Fact] void should_emit_what_the_retried_read_produced() => _observer.Values.ShouldContainOnly(2);
    [Fact] void should_not_end_the_observation() => _observer.HasFailed.ShouldBeFalse();
}
