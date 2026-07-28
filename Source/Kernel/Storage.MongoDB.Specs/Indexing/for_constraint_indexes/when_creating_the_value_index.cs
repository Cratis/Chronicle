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
public class when_creating_the_value_index(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    const string ConstraintName = "test-constraint";
    BsonDocument _valueIndex;

    async Task Because()
    {
        var storage = new UniqueConstraintsStorage(_database, EventSequenceId.Log, Substitute.For<ILogger<UniqueConstraintsStorage>>());
        await storage.IsAllowed((EventSourceId)"es-1", new UniqueConstraintDefinition(ConstraintName, []), (UniqueConstraintValue)"a-value");
        _valueIndex = await IndexFor($"{EventSequenceId.Log}+{ConstraintName}+constraint", "value");
    }

    [Fact] void should_create_the_value_index() => _valueIndex.ShouldNotBeNull();
    [Fact] void should_create_it_as_unique() => _valueIndex.GetValue("unique", false).ToBoolean().ShouldBeTrue();
}
