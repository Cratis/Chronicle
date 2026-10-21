// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_provisioning_as_a_contained_database_user;

/// <summary>
/// A contained database user - common with Azure SQL - exists only in its own database and cannot open master, where
/// the creation lock lives. Its already provisioned database must keep working.
/// </summary>
/// <param name="fixture">The <see cref="SqlServerFixture"/> owning the server.</param>
[Collection(SqlServerCollection.Name)]
public class and_the_database_exists(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    const string User = "contained_chronicle";
    const string Password = "Contained4476!Test";

    string _containedConnectionString;
    Exception _masterFailure;
    Exception _provisioningFailure;
    bool _targetOpened;

    async Task Establish()
    {
        var database = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
        await Execute(ConnectionString, "master", "EXEC sp_configure 'contained database authentication', 1; RECONFIGURE;");
        await Execute(ConnectionString, "master", $"ALTER DATABASE [{database}] SET CONTAINMENT = PARTIAL");
        await Execute(ConnectionString, database, $"CREATE USER [{User}] WITH PASSWORD = '{Password}'; ALTER ROLE db_owner ADD MEMBER [{User}];");

        _containedConnectionString = new SqlConnectionStringBuilder(ConnectionString) { UserID = User, Password = Password, Pooling = false }.ConnectionString;
        _masterFailure = await Catch.Exception(() => Open(new SqlConnectionStringBuilder(_containedConnectionString) { InitialCatalog = "master" }.ConnectionString));
    }

    async Task Because() => _provisioningFailure = await Catch.Exception(() => SqlServerDatabaseCreation.RunWithLock(
        _containedConnectionString,
        _ => Task.CompletedTask,
        async () =>
        {
            await Open(_containedConnectionString);
            _targetOpened = true;
        }));

    [Fact] void should_not_be_able_to_open_master() => ((SqlException)_masterFailure).Number.ShouldEqual(18456);
    [Fact] void should_not_fail() => _provisioningFailure.ShouldBeNull();
    [Fact] void should_check_the_database_without_the_lock() => _targetOpened.ShouldBeTrue();

    static async Task Open(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
    }

    static async Task Execute(string connectionString, string database, string sql)
    {
        await using var connection = new SqlConnection(new SqlConnectionStringBuilder(connectionString) { InitialCatalog = database }.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Spec-owned identifiers and constants only.
        command.CommandText = sql;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }
}
