// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.given;

public class a_sqlite_ledger : Specification
{
    SqliteConnection _keeper;
    private protected string _connectionString;
    readonly List<ReadModelTableBindingsDbContext> _contexts = [];
    private protected ReadModelTableBindingsDbContext _context;
    private protected ReadModelTableNameBindings _ledger;
    private protected ReadModelTableOwner _owner;

    void Establish()
    {
        _connectionString = $"Data Source=table_names_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _keeper = new SqliteConnection(_connectionString);
        _keeper.Open();
        _context = CreateContext();
        _ledger = CreateLedger(_context);
        _owner = new("store", "default", "model", "orders", ReadModelTableRole.Live, "orders");
    }

    void Destroy()
    {
        foreach (var context in _contexts)
        {
            context.Dispose();
        }
        _keeper.Dispose();
    }

    private protected ReadModelTableBindingsDbContext CreateContext()
    {
        var context = new ReadModelTableBindingsDbContext(new DbContextOptionsBuilder<ReadModelTableBindingsDbContext>().UseSqlite(_connectionString).Options);
        _contexts.Add(context);
        return context;
    }

    private protected static ReadModelTableNameBindings CreateLedger(ReadModelTableBindingsDbContext context) =>
        new(context, "main", new TableMigrator<ReadModelTableBindingsDbContext>(NullLogger<TableMigrator<ReadModelTableBindingsDbContext>>.Instance));

    private protected async Task<ReadModelTableNameBinding> Reserve(ReadModelTableOwner owner, string physical)
    {
        var row = ReadModelTableNameBinding.For(owner, physical, ReadModelTableBindingState.Reserved);
        return await _ledger.UnderLock(async () =>
        {
            (await _ledger.TryInsert(row)).ShouldBeTrue();
            return row;
        });
    }
}
