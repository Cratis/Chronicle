// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexProvisioner;

[Collection(MongoDBCollection.Name)]
public class when_migrating_an_index_with_a_conflicting_definition(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    MongoDBIndexReport _report;
    MongoDBIndexScopeResult _scope;
    BsonDocument _index;

    async Task Establish()
    {
        await _provisioner.Migrate([Store]);
        var collection = _namespace.GetCollection<BsonDocument>("event-log");
        await collection.Indexes.DropOneAsync("publication_identity");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument("publicationId", 1),
            new CreateIndexOptions { Name = "publication_identity", Unique = false, Sparse = false }));
        _commands.Clear();
    }

    async Task Because()
    {
        _report = await _provisioner.Migrate([Store]);
        _scope = _report.Scopes.Single(scope => scope.Database == _namespace.DatabaseNamespace.DatabaseName);
        _index = (await Indexes(_namespace, "event-log")).Single(index => index["name"] == "publication_identity");
    }

    [Fact] void should_not_rebuild_the_index() => DdlCommands.ShouldEqual(0);
    [Fact] void should_preserve_the_non_unique_index() => _index.GetValue("unique", false).AsBoolean.ShouldBeFalse();
    [Fact] void should_not_be_current() => _report.IsCurrent.ShouldBeFalse();
    [Fact] void should_report_the_index_as_incompatible() => _scope.Incompatible.ShouldContain("event-log/publication_identity");
}
