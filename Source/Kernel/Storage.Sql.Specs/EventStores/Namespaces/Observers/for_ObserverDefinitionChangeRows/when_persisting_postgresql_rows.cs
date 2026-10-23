// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverDefinitionChangeRows;

[Collection(PostgreSqlCollection.Name)]
public class when_persisting_postgresql_rows(PostgreSqlFixture fixture) : DefinitionChangeRowsContract
{
    string _connectionString;

    async Task Establish() => _connectionString = await fixture.CreateMigratedDatabase<NamespaceDbContext>();

    async Task Destroy() => await fixture.DropDatabase(_connectionString);

    protected override IObserverDefinitionChangeRows<T> OpenRows<T>() => new ObserverDefinitionChangeRows<T>(() => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), static () => { })));

    NamespaceDbContext CreateContext() => new(new DbContextOptionsBuilder<NamespaceDbContext>().UseNpgsql(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching().Options);
}
