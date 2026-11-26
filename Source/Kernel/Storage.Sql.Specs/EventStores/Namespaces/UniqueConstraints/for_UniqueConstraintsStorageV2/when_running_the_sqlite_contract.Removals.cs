// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

public partial class when_running_the_sqlite_contract
{
    [Fact]
    public async Task should_fence_removals_without_provisioning_a_binding()
    {
        await Initialize();
        (await _storage.Remove("owner", PerValue, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveValue("owner", PerValue, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveAllFor(PerValue, _authority)).IsSuccess.ShouldBeTrue();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM UniqueConstraintBindingsV2";
        Convert.ToInt64(await command.ExecuteScalarAsync()).ShouldEqual(0L);
        ShouldRefuse(await _storage.Remove("owner", PerValue, _scope, _authority with { Revision = new(1) }), UniqueConstraintWriteFailure.AuthorityMismatch);
    }

    [Fact]
    public async Task should_reject_null_removal_input_before_opening_a_context()
    {
        await Seed();
        var contexts = _optionsCreated;
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.RemoveAllFor(null!, _authority));
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.RemoveForSubject(_definition, null!, _authority));
        _optionsCreated.ShouldEqual(contexts);
    }
}
