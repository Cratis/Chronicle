// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.given;

/// <summary>
/// A container change stream over a database whose watches the spec controls, with a logger that records every use.
/// </summary>
public class a_container_change_stream : Specification
{
    protected const string ContainerName = "observed";

    protected static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);

    protected IMongoDatabase _database;
    protected ILogger<ReadModelChangeStreams> _logger;

    void Establish()
    {
        _database = Substitute.For<IMongoDatabase>();
        _database.DatabaseNamespace.Returns(new DatabaseNamespace("database"));
        _logger = Substitute.For<ILogger<ReadModelChangeStreams>>();
    }

    private protected ReadModelChangeStreams.ContainerChangeStream CreateStream() =>
        new(new(Substitute.For<IMongoClient>(), "database", ContainerName), _database, _logger, TimeProvider.System);

    protected void WatchWith(Func<CancellationToken, Task<IChangeStreamCursor<BsonDocument>>> watch) =>
        _database.WatchAsync(
                Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, BsonDocument>>(),
                Arg.Any<ChangeStreamOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(call => watch(call.ArgAt<CancellationToken>(2)));

    protected static Channel<bool> Subscriber() => Channel.CreateBounded<bool>(1);
}
