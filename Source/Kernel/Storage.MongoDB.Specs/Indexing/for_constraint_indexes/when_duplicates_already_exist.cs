// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_constraint_indexes;

[Collection(MongoDBCollection.Name)]
public class when_duplicates_already_exist(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    const string ConstraintName = "dup-constraint";
    BsonDocument _valueIndex;

    async Task Because()
    {
        var collectionName = $"{EventSequenceId.Log}+{ConstraintName}+constraint";
        var collection = _database.GetCollection<UniqueConstraintIndex>(collectionName);
        await collection.InsertManyAsync(
        [
            new UniqueConstraintIndex((EventSourceId)"es-1", (UniqueConstraintValue)"same-value", 0),
            new UniqueConstraintIndex((EventSourceId)"es-2", (UniqueConstraintValue)"same-value", 1)
        ]);

        var storage = new UniqueConstraintsStorage(_database, EventSequenceId.Log, Substitute.For<ILogger<UniqueConstraintsStorage>>());
        await storage.IsAllowed((EventSourceId)"es-3", new UniqueConstraintDefinition(ConstraintName, []), (UniqueConstraintValue)"another-value");

        _valueIndex = await IndexFor(collectionName, "value");
    }

    [Fact] void should_still_create_the_value_index() => _valueIndex.ShouldNotBeNull();
    [Fact] void should_fall_back_to_a_non_unique_index() => _valueIndex.GetValue("unique", false).ToBoolean().ShouldBeFalse();
}
