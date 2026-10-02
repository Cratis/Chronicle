// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

[Collection(MongoDBCollection.Name)]
public class and_a_real_collection_with_default_collation_has_a_legacy_named_index(MongoDBFixture fixture) : given.a_sink_with_a_real_legacy_index(fixture)
{
    Exception _error;
    IReadOnlyList<string> _indexes;
    BsonDocument _legacyIndex;

    async Task Establish() => await CreateLegacyIndex(new Collation("en", strength: CollationStrength.Secondary));

    async Task Because()
    {
        _error = await Catch.Exception(_sink.EnsureIndexes);
        _indexes = await IndexNamesFor(CollectionName);
        _legacyIndex = await IndexFor(CollectionName, "p_1");
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_keep_the_legacy_index() => _indexes.ShouldContain("p_1");
    [Fact] void should_not_create_a_chronicle_named_index() => _indexes.ShouldNotContain("chronicle_idx_p");
    [Fact] void should_preserve_the_inherited_collation() => _legacyIndex["collation"]["locale"].AsString.ShouldEqual("en");
}
