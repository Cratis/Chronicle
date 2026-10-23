// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverDefinitionChangeRows;

[Collection(PostgreSqlCollection.Name)]
public class when_persisting_postgresql_rows(PostgreSqlFixture fixture) : DefinitionChangeRowsContract
{
    string _connectionString;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    async Task Destroy() => await fixture.DropDatabase(_connectionString);

    protected override IObserverDefinitionChangeRows<T> OpenRows<T>() => new ObserverDefinitionChangeRows<T>(() => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), static () => { })));

    /// <summary>
    /// Creates a context with the kernel's own option set and EF Core's provider cache kept on. The rows open many
    /// contexts, so an uncached provider would rebuild the model for every context, and matching the kernel's options
    /// reuses its cached provider instead of adding one more towards EF's limit of twenty.
    /// </summary>
    NamespaceDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>();
        builder.UseDatabaseFromConnectionString(_connectionString).AddConceptAsSupport();
        return new(builder.Options);
    }
}
