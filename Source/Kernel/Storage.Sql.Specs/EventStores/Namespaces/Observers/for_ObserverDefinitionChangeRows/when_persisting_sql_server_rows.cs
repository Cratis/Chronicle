// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverDefinitionChangeRows;

[Collection(SqlServerCollection.Name)]
public class when_persisting_sql_server_rows(SqlServerFixture fixture) : DefinitionChangeRowsContract
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

    NamespaceDbContext CreateContext() => new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlServer(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching().Options);
}
