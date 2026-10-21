// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverDefinitionChangeRows;

public class when_persisting_rows : DefinitionChangeRowsContract, IDisposable
{
    SqliteConnection _connection;
    string _connectionString;

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    async Task Establish()
    {
        _connectionString = $"DataSource=definition_rows_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _connection = new(_connectionString);
        await _connection.OpenAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    protected override IObserverDefinitionChangeRows<T> OpenRows<T>() => new ObserverDefinitionChangeRows<T>(() => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), static () => { })));

    NamespaceDbContext CreateContext() => new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching().Options);
}
