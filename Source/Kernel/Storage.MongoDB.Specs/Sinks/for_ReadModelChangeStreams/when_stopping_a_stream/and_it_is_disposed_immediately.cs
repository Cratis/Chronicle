// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_stopping_a_stream;

/// <summary>
/// A stream disposed right after it was started ends its watch without a fault: the token it stops on is taken when
/// it starts, not when the thread pool gets round to running the watch, by which time the source may be long gone.
/// </summary>
public class and_it_is_disposed_immediately : given.a_container_change_stream
{
    const int Streams = 200;

    Exception _error;
    Channel<bool>[] _subscribers;

    void Establish() =>
        WatchWith(_ => Task.FromResult<IChangeStreamCursor<BsonDocument>>(new given.a_controllable_cursor()));

    async Task Because()
    {
        _subscribers = [.. Enumerable.Range(0, Streams).Select(_ => Subscriber())];
        var watches = new List<Task>();
        foreach (var subscriber in _subscribers)
        {
            var stream = CreateStream();
            stream.Add(subscriber.Writer);
            watches.Add(stream.Start());
            stream.Dispose();
        }

        _error = await Catch.Exception(() => Task.WhenAll(watches).WaitAsync(_deadline));
    }

    [Fact] void should_end_every_watch_without_a_fault() => _error.ShouldBeNull();
    [Fact] void should_not_fail_any_subscriber() => _subscribers.Any(subscriber => subscriber.Reader.Completion.IsCompleted).ShouldBeFalse();
}
