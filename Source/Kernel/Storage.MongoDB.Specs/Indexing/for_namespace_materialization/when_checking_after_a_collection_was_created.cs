// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_namespace_materialization;

/// <summary>
/// The moment a namespace's storage actually gets used for something - here, the event log's indexes getting
/// ensured, the same lazy path a genuine first append goes through - the database is materialized and must be
/// reported as having data from then on.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_checking_after_a_collection_was_created(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    bool _hasAnyCollections;

    async Task Because()
    {
        await _database.EnsureIndexesForEventSequence(EventSequenceId.Log);
        _hasAnyCollections = await _database.HasAnyCollections();
    }

    [Fact] void should_report_having_collections() => _hasAnyCollections.ShouldBeTrue();
}
