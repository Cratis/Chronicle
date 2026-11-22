// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

public partial class when_running_the_sqlite_contract
{
    [Fact]
    public async Task should_keep_every_physical_identifier_within_63_utf8_bytes()
    {
        await Seed();
        var definition = PerValue with { Name = new string('界', 400) + "\"'--" };
        (await _storage.InitializeAuthority(definition.Name, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_owner, definition, 10, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        foreach (var name in (await Catalog()).Select(row => row[..row.IndexOf(':')]))
        {
            Encoding.UTF8.GetByteCount(name).ShouldBeLessThanOrEqual(63);
        }
    }

    [Fact]
    public async Task should_fail_closed_on_a_logical_binding_mismatch()
    {
        await Seed();
        await Execute(command =>
        {
            command.CommandText = "UPDATE UniqueConstraintBindingsV2 SET \"Sequence\" = @wrong";
            command.Parameters.AddWithValue("@wrong", "another-sequence");
        });
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.GetFor(_owner, _definition, _scope));
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.Save(_owner, _definition, 11, _otherValue, _scope, _authority));
    }

    [Fact]
    public async Task should_fail_closed_on_a_physical_binding_mismatch()
    {
        await Seed();
        await Execute(command =>
        {
            command.CommandText = "UPDATE UniqueConstraintBindingsV2 SET \"Table\" = @wrong";
            command.Parameters.AddWithValue("@wrong", "arbitrary_table");
        });
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.GetFor(_owner, _definition, _scope));
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.Save(_owner, _definition, 11, _otherValue, _scope, _authority));
    }

    [Fact]
    public async Task should_refuse_to_adopt_an_unbound_existing_table()
    {
        await Seed();
        var before = await Catalog();
        await Execute(command => command.CommandText = "DELETE FROM UniqueConstraintBindingsV2");
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.Save(_owner, _definition, 11, _otherValue, _scope, _authority));
        (await Catalog()).SequenceEqual(before).ShouldBeTrue();
    }

    [Fact]
    public async Task should_roll_back_table_creation_when_binding_publication_fails_and_allow_retry()
    {
        await Initialize();
        await Execute(command => command.CommandText = "CREATE TRIGGER refuse_binding BEFORE INSERT ON UniqueConstraintBindingsV2 BEGIN SELECT RAISE(ABORT, 'injected binding failure'); END");
        var before = await Catalog();
        await Assert.ThrowsAsync<SqliteException>(() => _storage.Save(_owner, _definition, 10, _value, _scope, _authority));
        (await Catalog()).SequenceEqual(before).ShouldBeTrue();
        await Execute(command => command.CommandText = "DROP TRIGGER refuse_binding");
        _storage = await ReopenStorage();
        (await _storage.Save(_owner, _definition, 10, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldRetainClaim();
    }
}
