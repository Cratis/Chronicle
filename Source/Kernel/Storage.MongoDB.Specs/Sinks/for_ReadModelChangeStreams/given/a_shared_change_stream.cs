// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.given;

/// <summary>
/// Change streams over a database whose every watch opens a <see cref="a_controllable_cursor"/>, observed by a read
/// that counts itself.
/// </summary>
public class a_shared_change_stream : Specification
{
    protected const string ContainerName = "observed";

    protected IMongoDatabase _database;
    protected ReadModelChangeStreams _changeStreams;
    protected Func<Task<IChangeStreamCursor<BsonDocument>>> _watch;
    protected Func<int, Task<int>> _read;
    readonly Lock _lock = new();
    readonly List<a_controllable_cursor> _cursors = [];
    readonly List<CancellationToken> _watchTokens = [];
    int _watches;
    int _reads;

    protected int Watches => Volatile.Read(ref _watches);
    protected int Reads => Volatile.Read(ref _reads);

    void Establish()
    {
        _database = Substitute.For<IMongoDatabase>();
        _database.DatabaseNamespace.Returns(new DatabaseNamespace("database"));
        _database.WatchAsync(
                Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, BsonDocument>>(),
                Arg.Any<ChangeStreamOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                Interlocked.Increment(ref _watches);
                lock (_lock)
                {
                    _watchTokens.Add(call.ArgAt<CancellationToken>(2));
                }

                return _watch();
            });

        _watch = () =>
        {
            var cursor = new a_controllable_cursor();
            lock (_lock)
            {
                _cursors.Add(cursor);
            }

            return Task.FromResult<IChangeStreamCursor<BsonDocument>>(cursor);
        };
        _read = Task.FromResult;
        _changeStreams = new(NullLogger<ReadModelChangeStreams>.Instance, new an_immediate_time_provider());
    }

    protected IObservable<int> Observe() =>
        _changeStreams.Observe(_database, ContainerName, _ => _read(Interlocked.Increment(ref _reads)));

    protected a_controllable_cursor Cursor(int index)
    {
        lock (_lock)
        {
            return _cursors[index];
        }
    }

    protected CancellationToken WatchToken(int index)
    {
        lock (_lock)
        {
            return _watchTokens[index];
        }
    }
}
