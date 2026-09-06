// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_beginning_a_replay;

/// <summary>
/// A replay fills a shadow collection that is renamed into place when it finishes, and a rename carries only the
/// indexes that collection has of its own. The declared indexes therefore have to be created on it as the replay
/// begins, or the read model comes back from a replay unindexed (#3942).
/// </summary>
public class and_the_read_model_declares_indexes : given.a_sink_with_indexes
{
    void Establish() => _indexCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

    async Task Because() => await _sink.BeginReplay(
        new ReplayContext(
            new ReadModelType("SomethingId", ReadModelGeneration.First),
            "Something",
            "revert-Something",
            DateTimeOffset.UtcNow));

    [Fact] void should_begin_the_replay_on_the_collections() => _collections.Received(1).BeginReplay(Arg.Any<ReplayContext>());

    [Fact] void should_create_the_declared_index() =>
        _indexManager.Received(1).CreateOneAsync(
            Arg.Is<CreateIndexModel<BsonDocument>>(model =>
                model.Options.Name == $"chronicle_idx_{_indexedProperty.Path}"),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
}
