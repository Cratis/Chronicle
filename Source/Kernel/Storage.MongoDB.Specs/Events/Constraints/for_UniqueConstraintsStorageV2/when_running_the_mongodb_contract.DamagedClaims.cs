// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorageV2;

public partial class when_running_the_mongodb_contract
{
    [Theory]
    [InlineData("EventSourceId", false)]
    [InlineData("EventSourceId", true)]
    [InlineData("SequenceNumber", false)]
    [InlineData("SequenceNumber", true)]
    public async Task should_report_missing_or_wrongly_typed_claim_proofs_as_schema_damage(string field, bool missing)
    {
        await Seed();
        var update = missing ? new BsonDocument("$unset", new BsonDocument(field, 1)) : new BsonDocument("$set", new BsonDocument(field, 123));
        await _database.GetCollection<BsonDocument>(Binding().Physical).UpdateManyAsync(FilterDefinition<BsonDocument>.Empty, update);
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.IsAllowed(_owner, _definition, _value, _scope));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_report_missing_or_wrongly_typed_claim_tokens_as_schema_damage(bool missing)
    {
        await Seed();
        var update = missing ? new BsonDocument("$unset", new BsonDocument("Value", 1)) : new BsonDocument("$set", new BsonDocument("Value", 123));
        await _database.GetCollection<BsonDocument>(Binding().Physical).UpdateManyAsync(FilterDefinition<BsonDocument>.Empty, update);
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.GetFor(_owner, _definition, _scope));
    }

    [Fact]
    public async Task should_report_an_invalid_persisted_token_as_schema_damage()
    {
        await Seed();
        await _database.GetCollection<BsonDocument>(Binding().Physical).UpdateManyAsync(FilterDefinition<BsonDocument>.Empty, new BsonDocument("$set", new BsonDocument("Value", "not-a-token")));
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.GetFor(_owner, _definition, _scope));
    }
}
