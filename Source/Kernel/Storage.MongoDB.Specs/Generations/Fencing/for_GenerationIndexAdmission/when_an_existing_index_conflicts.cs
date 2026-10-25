// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_GenerationIndexAdmission;

public class when_an_existing_index_conflicts : Sinks.for_Sink.given.a_sink_with_indexes
{
    Exception? _error;

    void Establish()
    {
        typeof(Sink).GetProperty("GenerationIndexDdl", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_sink, (Func<Func<Task>, Task>)(operation => operation()));
        _indexCursor.Current.Returns([new BsonDocument { { "name", "chronicle_idx_SomeProperty" }, { "key", new BsonDocument("different", 1) } }]);
        _indexCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
    }

    async Task Because() => _error = await Catch.Exception(_sink.EnsureIndexes);

    [Fact] void should_report_a_typed_conflict() => _error.ShouldBeOfExactType<GenerationIndexConflict>();
    [Fact] void should_never_drop_the_conflicting_index() => _indexManager.DidNotReceive().DropOneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_never_replace_it() => _indexManager.DidNotReceive().CreateOneAsync(Arg.Any<CreateIndexModel<BsonDocument>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>());
}
