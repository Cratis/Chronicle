// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionHistoryStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_reading_an_unknown_projection(ReplicaSetMongoDBFixture fixture) : given.a_history_storage(fixture)
{
    Task Because() => Read();
    [Fact] void should_return_no_history() => _entries.ShouldBeEmpty();
}
