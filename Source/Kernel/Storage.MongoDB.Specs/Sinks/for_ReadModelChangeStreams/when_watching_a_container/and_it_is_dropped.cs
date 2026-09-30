// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_watching_a_container;

/// <summary>
/// Dropping the container really does empty it, so unlike renaming it aside that is reported.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/> supplying the replica set the change stream needs.</param>
[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_it_is_dropped(ReplicaSetMongoDBFixture fixture) : given.a_watched_container(fixture.ConnectionString)
{
    string[] _operations;

    async Task Because()
    {
        await _database.DropCollectionAsync(ContainerName);
        await InsertMarker();
        _operations = await OperationsUpToTheMarker();
    }

    [Fact] void should_report_the_drop() => _operations.ShouldEqual(["drop", "insert"]);
}
