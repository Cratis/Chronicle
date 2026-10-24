// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverDefinitionChangeRows;

public class when_persisting_rows : DefinitionChangeRowsContract, IDisposable
{
    static readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();

    SqliteConnection _connection;
    string _connectionString;

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    async Task Establish()
    {
        _connectionString = $"Data Source=definition_rows_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _connection = new(_connectionString);
        await _connection.OpenAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    protected override IObserverDefinitionChangeRows<T> OpenRows<T>() => new ObserverDefinitionChangeRows<T>(() => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), static () => { })));

    /// <summary>
    /// Creates a context through the kernel's own option builder, with EF Core's provider cache kept on. The rows open
    /// many contexts, so an uncached provider would rebuild the model for every context, and building the options the
    /// way the kernel does reuses its cached provider instead of adding one more towards EF's limit of twenty.
    /// </summary>
    NamespaceDbContext CreateContext() => new(Database.BuildOptions<NamespaceDbContext>(_services, _connectionString));
}
