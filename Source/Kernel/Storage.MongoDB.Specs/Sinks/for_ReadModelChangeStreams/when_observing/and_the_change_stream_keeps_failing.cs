// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_observing;

/// <summary>
/// A change stream that cannot be opened however often it is retried ends the observation with the failure.
/// </summary>
public class and_the_change_stream_keeps_failing : given.a_shared_change_stream
{
    given.a_recording_observer<int> _observer;
    Exception _error;

    void Establish() =>
        _watch = () => Task.FromException<IChangeStreamCursor<BsonDocument>>(new MongoClientException("The server is unreachable"));

    async Task Because()
    {
        _observer = new();
        using var subscription = Observe().Subscribe(_observer);
        _error = await _observer.WaitForError();
    }

    [Fact] void should_end_the_observation_with_the_failure() => _error.ShouldBeOfExactType<MongoClientException>();
    [Fact] void should_give_up_after_the_bounded_number_of_attempts() => Watches.ShouldEqual(ReadModelChangeStreams.MaxConsecutiveFailures);
    [Fact] void should_not_read() => _observer.Values.ShouldBeEmpty();
}
