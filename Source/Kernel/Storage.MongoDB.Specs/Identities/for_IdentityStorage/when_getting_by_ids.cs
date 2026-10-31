// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_IdentityStorage;

[Collection(MongoDBCollection.Name)]
public class when_getting_by_ids(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    IdentityStorage _reader;
    IdentityId _id;
    IReadOnlyDictionary<IdentityId, Identity> _result;

    async Task Establish()
    {
        _id = IdentityId.New();
        await _database.GetCollection<MongoDBIdentity>(WellKnownCollectionNames.Identities).InsertOneAsync(new MongoDBIdentity
        {
            Id = _id.Value,
            Subject = "person",
            Name = "Old name",
            UserName = "username"
        });
        _reader = new IdentityStorage(_database, Substitute.For<ILogger<IdentityStorage>>());
        await _reader.Populate();
        var writer = new IdentityStorage(_database, Substitute.For<ILogger<IdentityStorage>>());
        await writer.Rename("person", "Current name");
    }

    async Task Because() => _result = await _reader.GetByIds([_id, IdentityId.New(), _id]);

    [Fact] void should_omit_missing_ids() => _result.Keys.ShouldContainOnly(_id);
    [Fact] void should_bypass_the_stale_reader_cache() => _result[_id].Name.ShouldEqual("Current name");
}
