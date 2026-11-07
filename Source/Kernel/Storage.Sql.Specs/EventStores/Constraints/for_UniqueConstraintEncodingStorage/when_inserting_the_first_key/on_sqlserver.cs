// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_UniqueConstraintEncodingStorage.when_inserting_the_first_key;

[Collection(SqlServerCollection.Name)]
public class on_sqlserver(SqlServerFixture fixture) : given.a_sqlserver_key_storage(fixture)
{
    UniqueConstraintEncodingKey _candidate;
    UniqueConstraintEncodingKey _result;
    UniqueConstraintEncodingKey _stored;
    string _columnShape;

    async Task ReadColumnShape()
    {
        await using var scope = await Database.EventStore(Store);
        await using var connection = new SqlConnection(scope.DbContext.Database.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT STRING_AGG(COLUMN_NAME + ':' + DATA_TYPE + ':' + CAST(CHARACTER_MAXIMUM_LENGTH AS varchar(10)), ',') WITHIN GROUP (ORDER BY COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = SCHEMA_NAME() AND TABLE_NAME = @t AND COLUMN_NAME IN ('KeyId', 'Material')", connection);
        command.Parameters.AddWithValue("@t", "UniqueConstraintEncodingKey");
        _columnShape = (string)(await command.ExecuteScalarAsync())!;
    }

    void Establish()
    {
        // Contains 0x00 and 0xFF at the edges to prove exact binary round trips.
        var material = new byte[32];
        material[0] = 0x00;
        material[1] = 0xFF;
        material[31] = 0xFF;
        _candidate = new UniqueConstraintEncodingKey(new UniqueConstraintStoreKeyId("00ff10a5b6c7d8e9f0123456789abcde"), new UniqueConstraintStoreKey(material));
    }

    async Task Because()
    {
        _result = await _storage.CreateKeyIfAbsent(_candidate);
        _stored = (await _storage.GetKey())!;
        await ReadColumnShape();
    }

    [Fact] void should_return_the_candidate() => _result.ShouldEqual(_candidate);
    [Fact] void should_read_the_same_key_id() => _stored.KeyId.ShouldEqual(_candidate.KeyId);
    [Fact] void should_read_identical_material() => Assert.Equal(_candidate.Material.CopyBytes(), _stored.Material.CopyBytes());
    [Fact] void should_use_fixed_length_binary_columns() => _columnShape.ShouldEqual("KeyId:binary:16,Material:binary:32");
}
