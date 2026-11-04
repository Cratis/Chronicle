// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_PrimaryKeyConstraints.when_reading_primary_key_columns.given;

/// <summary>
/// A database holding a table whose primary key is a synthesized <c language="csharp">Id</c> column beside an <c language="csharp">ID</c> column.
/// </summary>
public abstract class a_database_with_a_legacy_table : Specification
{
    protected const string Table = "legacy_models";
    protected const string CompositeTable = "composite_models";
    private protected TableLayout _legacy;
    private protected TableLayout _composite;
    private protected TableLayout _missing;
    string _connectionString;

    /// <summary>
    /// Gets the SQL creating the table with the synthesized key.
    /// </summary>
    protected abstract string CreateLegacyTable { get; }

    /// <summary>
    /// Gets the SQL creating a table with a composite primary key.
    /// </summary>
    protected abstract string CreateCompositeTable { get; }

    protected abstract Task<string> CreateDatabase();

    protected abstract Task Execute(string connectionString, string sql);

    protected abstract Task DropDatabase(string connectionString);

    async Task Establish()
    {
        _connectionString = await CreateDatabase();
        await Execute(_connectionString, CreateLegacyTable);
        await Execute(_connectionString, CreateCompositeTable);
    }

    protected async Task ReadLayouts()
    {
        await using var context = new DbContext(SpecDbContextOptions.ForConnection<DbContext>(_connectionString));
        var type = context.Database.GetDatabaseType();
        _legacy = await PrimaryKeyColumns.LayoutOf(context.Database, type, Table);
        _composite = await PrimaryKeyColumns.LayoutOf(context.Database, type, CompositeTable);
        _missing = await PrimaryKeyColumns.LayoutOf(context.Database, type, "missing_models");
    }

    async Task Destroy() => await DropDatabase(_connectionString);
}
