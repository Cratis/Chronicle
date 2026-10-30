// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Storage.Sql.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.given;

/// <summary>
/// Sets up a named shared-cache in-memory SQLite database whose schema comes from the namespace
/// migrations the kernel applies, not from the EF Core model. A schema built with
/// <c language="csharp">EnsureCreated</c> always agrees with the model, so it cannot reveal a table or
/// column the migrations never create.
/// </summary>
public class a_migrated_namespace_database : Specification, IDisposable
{
    protected static readonly EventStoreName _eventStore = "test-store";
    protected static readonly EventStoreNamespaceName _namespace = "test-namespace";
    protected IDatabase _database;
    SqliteConnection _connection;
    string _connectionString;
    readonly IntegrationTimings _timings = new(nameof(a_migrated_namespace_database), "sql-specs", "sqlite");
    SqlCommandTimings? _commandTimings;

    void Establish()
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"namespace-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        // Keeps the named in-memory database alive while each scope opens its own connection.
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        using (var migrationContext = CreateContext())
        {
            _timings.Measure("ef-migration", () =>
            {
                migrationContext.Database.Migrate();
                return true;
            });
        }

        _database = Substitute.For<IDatabase>();
        _database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => new DbContextScope<NamespaceDbContext>(CreateContext(), () => { }));
    }

    protected NamespaceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NamespaceDbContext>()
            .UseSqlite(_connectionString)
            .AddConceptAsSupport().WithoutServiceProviderCaching()
            .Options;

        if (IntegrationTimings.Enabled && _commandTimings is null)
        {
            _commandTimings = new(_timings);
            _commandTimings.Start();
        }
        return _timings.Measure("ef-context-construction", () =>
        {
            var context = new NamespaceDbContext(options);
            return _commandTimings?.Track(context) ?? context;
        });
    }

    protected IEnumerable<string> ColumnsOf(string table)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT name FROM pragma_table_info($table)";
        command.Parameters.AddWithValue("$table", table);
        using var reader = command.ExecuteReader();
        var columns = new List<string>();
        while (reader.Read())
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    public void Dispose()
    {
        try
        {
            _connection.Dispose();
            GC.SuppressFinalize(this);
        }
        finally
        {
            _commandTimings?.Dispose();
            _timings.Complete();
        }
    }
}
