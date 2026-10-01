// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.given;

public class an_isolated_replay : Specification
{
    protected readonly HashSet<string> _names = [];
    protected SinkCollections _collections;
    protected IMongoDatabase _database;
    protected ReplayContext _context;
    void Establish()
    {
        _database = Substitute.For<IMongoDatabase>();
        _database.ListCollectionNamesAsync(Arg.Any<ListCollectionNamesOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var options = call.Arg<ListCollectionNamesOptions>();
            var filter = ((BsonDocumentFilterDefinition<BsonDocument>)options.Filter).Document;
            var name = filter["name"].AsString;
            var cursor = Substitute.For<IAsyncCursor<string>>();
            cursor.Current.Returns(_names.Contains(name) ? [name] : []);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
            return Task.FromResult(cursor);
        });
        _database.RenameCollectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<RenameCollectionOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _names.Remove(call.ArgAt<string>(0));
            _names.Add(call.ArgAt<string>(1));
            return Task.CompletedTask;
        });
        var definition = new ReadModelDefinition("model", "Model", "Model", ReadModelOwner.None, ReadModelSource.Code, ReadModelObserverType.Reducer, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema>(), []);
        _collections = new(definition, _database);
        _context = new(new("model", 1), "Model", "Revert", DateTimeOffset.UtcNow) { ReplayContainerName = "isolated", AllowEmptyResult = true };
    }
}
