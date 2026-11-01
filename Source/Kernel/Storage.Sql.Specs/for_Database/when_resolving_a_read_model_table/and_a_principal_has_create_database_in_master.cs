// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_a_read_model_table;

/// <summary>
/// A principal with CREATE DATABASE in master sees every database, so a store whose database does not exist is proved absent and a new name is reserved.
/// </summary>
/// <param name="fixture">The <see cref="SqlServerFixture"/> owning the server.</param>
[Collection(SqlServerCollection.Name)]
public class and_a_principal_has_create_database_in_master(SqlServerFixture fixture) : given.a_sql_server_principal_with_hidden_databases(fixture, "CREATE USER [{login}] FOR LOGIN [{login}]; GRANT CREATE DATABASE TO [{login}]", false)
{
    Exception _error;
    ReadModelTableNameBinding _binding;

    async Task Because()
    {
        _error = await Catch.Exception(async () =>
        {
            await using var scope = await _limited.Tables.Open(_new, _columns);
        });
        _binding = await _limited.Tables.Find(_new);
    }

    [Fact] void should_reserve_the_new_name() => _error.ShouldBeNull();
    [Fact] void should_bind_the_new_name() => _binding.ShouldNotBeNull();
}
