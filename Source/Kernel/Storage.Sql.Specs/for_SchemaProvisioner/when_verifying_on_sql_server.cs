// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

[Collection(SqlServerCollection.Name)]
public class when_verifying_on_sql_server(SqlServerFixture fixture) : Specification
{
    string _connectionString;
    ServiceProvider _services;
    bool _reviewTableCreatedByMigrate;
    bool _reviewTableExistsAfterVerify;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        _services = given.schema_services.Create(new ChronicleOptions
        {
            Storage = new()
            {
                Type = StorageType.MsSql,
                ConnectionDetails = _connectionString,
                SchemaMode = SchemaMode.Migrate,
                CreateIfMissing = true
            }
        });
        var provisioner = _services.GetRequiredService<ISchemaProvisioner>();
        await provisioner.Migrate(new(["orders"], ["production"]));
        _reviewTableCreatedByMigrate = await ReviewTableExists();
        await Drop();
    }

    async Task Because()
    {
        await _services.GetRequiredService<ISchemaProvisioner>().Verify(new(["orders"], ["production"]));
        _reviewTableExistsAfterVerify = await ReviewTableExists();
    }

    async Task Destroy()
    {
        await _services.DisposeAsync();
        await fixture.DropDatabase(_connectionString);
    }

    [Fact] void should_have_created_the_review_table_when_migrating() => _reviewTableCreatedByMigrate.ShouldBeTrue();
    [Fact] void should_not_create_the_review_table_when_verifying() => _reviewTableExistsAfterVerify.ShouldBeFalse();

    async Task<bool> ReviewTableExists()
    {
        await using var connection = new SqlConnection(await FindNamespaceDatabase());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{EventSequenceMigrator.SpellingReviewsTable}'";
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    async Task Drop()
    {
        await using var connection = new SqlConnection(await FindNamespaceDatabase());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP TABLE [{EventSequenceMigrator.SpellingReviewsTable}]";
        await command.ExecuteNonQueryAsync();
    }

    async Task<string> FindNamespaceDatabase()
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        var prefix = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        await using var master = new SqlConnection(builder.ConnectionString);
        await master.OpenAsync();
        await using var command = master.CreateCommand();
        command.CommandText = "SELECT name FROM sys.databases WHERE name LIKE @prefix + '%'";
        command.Parameters.AddWithValue("@prefix", prefix);
        var names = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }
        }

        foreach (var name in names)
        {
            builder.InitialCatalog = name;
            await using var candidate = new SqlConnection(builder.ConnectionString);
            await candidate.OpenAsync();
            await using var probe = candidate.CreateCommand();
            probe.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'event-log'";
            if (Convert.ToInt32(await probe.ExecuteScalarAsync()) > 0)
            {
                return builder.ConnectionString;
            }
        }

        throw new InvalidOperationException("The namespace database was not found among " + string.Join(',', names));
    }
}
