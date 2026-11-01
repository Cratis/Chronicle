// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionHistoryStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_inserting_into_a_read_only_view(ReplicaSetMongoDBFixture fixture) : given.a_history_storage(fixture)
{
    Exception _exception;
    async Task Establish()
    {
        await _database.CreateCollectionAsync("source");
        await _database.CreateViewAsync<ProjectionDefinitionHistoryDocument, ProjectionDefinitionHistoryDocument>(
            WellKnownCollectionNames.ProjectionDefinitionHistory, "source", Array.Empty<BsonDocument>());
    }
    async Task Because() => _exception = await Catch.ExceptionAsync(() => _storage.Record(_entry));
    [Fact] void should_propagate_non_duplicate_storage_failures() => _exception.ShouldNotBeNull();
}
