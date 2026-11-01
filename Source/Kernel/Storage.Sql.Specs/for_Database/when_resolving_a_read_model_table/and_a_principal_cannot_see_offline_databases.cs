// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_a_read_model_table;

/// <summary>
/// A principal that is not sysadmin cannot prove an OFFLINE legacy database is absent, so it must not reserve a new name
/// but must keep serving a Ready binding.
/// </summary>
/// <param name="fixture">The <see cref="SqlServerFixture"/> owning the server.</param>
[Collection(SqlServerCollection.Name)]
public class and_a_principal_cannot_see_offline_databases(SqlServerFixture fixture) : given.a_sql_server_principal_with_hidden_databases(fixture, string.Empty, true)
{
    Exception _newError;
    Exception _readyError;
    ReadModelTableNameBinding _newBinding;
    bool _tableCreated;

    async Task Because()
    {
        _newError = await Catch.Exception(async () =>
        {
            await using var scope = await _limited.Tables.Open(_new, _columns);
        });
        _newBinding = await _limited.Tables.Find(_new);
        _limited.Tables.Clear();
        _readyError = await Catch.Exception(async () =>
        {
            await using var scope = await _limited.Tables.Open(_ready, _columns);
            (await scope.DbContext.Entries.CountAsync()).ShouldEqual(0);
            await scope.DbContext.Database.OpenConnectionAsync();
            await using var command = scope.DbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = 'orders'";
            _tableCreated = Convert.ToInt32(await command.ExecuteScalarAsync()) != 0;
        });
    }

    [Fact] void should_refuse_the_new_name() => _newError.ShouldBeOfExactType<ReadModelTableMetadataUnavailable>();
    [Fact] void should_not_reserve_or_adopt() => _newBinding.ShouldBeNull();
    [Fact] void should_keep_serving_the_ready_binding() => _readyError.ShouldBeNull();
    [Fact] void should_not_create_a_table() => _tableCreated.ShouldBeFalse();
}
