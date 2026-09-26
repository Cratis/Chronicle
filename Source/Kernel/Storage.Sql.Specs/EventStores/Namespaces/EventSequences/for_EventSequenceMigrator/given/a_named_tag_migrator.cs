// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator.given;

/// <summary>Real SQLite migration against an isolated namespace database.</summary>
public class a_named_tag_migrator : Specification, IDisposable
{
    protected DbConnection _connection;
    protected string? _provider;
    string _connectionString;
    EventSequenceMigrator _migrator;

    void Establish()
    {
        _provider = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_PROVIDER");
        var configured = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_CONNECTION_STRING");
        var name = $"nt_{Guid.NewGuid():N}";
        if (_provider == "PostgreSQL")
        {
            var builder = new NpgsqlConnectionStringBuilder(configured) { Database = name };
            _connectionString = builder.ConnectionString;
            CreateDatabase(new NpgsqlConnection(configured), $"CREATE DATABASE \"{name}\"");
            _connection = new NpgsqlConnection(_connectionString);
        }
        else if (_provider == "SQLServer")
        {
            var builder = new SqlConnectionStringBuilder(configured) { InitialCatalog = name };
            _connectionString = builder.ConnectionString;
            CreateDatabase(new SqlConnection(configured), $"CREATE DATABASE [{name}]");
            _connection = new SqlConnection(_connectionString);
        }
        else
        {
            _connectionString = $"DataSource={name};Mode=Memory;Cache=Shared";
            _connection = new SqliteConnection(_connectionString);
        }
        _connection.Open();
        _migrator = new EventSequenceMigrator(
            new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>()),
            Substitute.For<ILogger<EventSequenceMigrator>>());
    }

    protected EventSequenceDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>();
        switch (_provider)
        {
            case "PostgreSQL": builder.UseNpgsql(_connectionString); break;
            case "SQLServer": builder.UseSqlServer(_connectionString); break;
            default: builder.UseSqlite(_connectionString); break;
        }
        builder.AddConceptAsSupport();
        return new EventSequenceDbContext(builder.Options, "event-sequence", _migrator);
    }

    protected async Task Execute(string sql)
    {
        await using var command = _connection.CreateCommand();
#pragma warning disable CA2100 // Only hard-coded schema fixtures call this test helper.
        command.CommandText = sql;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    protected async Task<bool> Exists(string name)
    {
        await using var command = _connection.CreateCommand();
#pragma warning disable CA2100 // Fixed provider-specific schema queries, with a parameterized table name.
        command.CommandText = _provider switch
        {
            "PostgreSQL" => "SELECT COUNT(*) FROM information_schema.tables WHERE table_name=@name",
            "SQLServer" => "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME=@name",
            _ => "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name"
        };
#pragma warning restore CA2100
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@name";
        parameter.Value = name;
        command.Parameters.Add(parameter);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    static void CreateDatabase(DbConnection connection, string sql)
    {
        using (connection)
        {
            connection.Open();
            using var command = connection.CreateCommand();
#pragma warning disable CA2100 // The database name is a locally generated GUID, never user input.
            command.CommandText = sql;
#pragma warning restore CA2100
            command.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
