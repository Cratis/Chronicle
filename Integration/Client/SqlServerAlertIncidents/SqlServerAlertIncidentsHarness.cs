// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Alerts;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

using KernelEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using KernelNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;

namespace Cratis.Chronicle.Kernel.Integration.Storage.Sql.Alerts;

/// <summary>
/// Supplies real SQL Server execution of the shared incident storage contract.
/// </summary>
public class SqlServerAlertIncidentsHarness : IAlertIncidentsStorageHarness
{
    string _connectionString = null!;

    /// <summary>
    /// Gets or sets the SQL Server fixture.
    /// </summary>
    public SqlServerFixture Fixture { get; set; } = null!;

    /// <inheritdoc/>
    public async Task<IAlertIncidentsStorage> Create()
    {
        _connectionString = await Fixture.CreateDatabase();
        await using (var context = Context())
        {
            await context.Database.MigrateAsync();
        }
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<KernelEventStoreName>(), Arg.Any<KernelNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(Context(), () => { })));

        return new AlertIncidentsStorage(KernelEventStoreName.System, KernelNamespaceName.Default, database);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await Fixture.DropDatabase(_connectionString);
        GC.SuppressFinalize(this);
    }

    NamespaceDbContext Context() => new(new DbContextOptionsBuilder<NamespaceDbContext>()
        .UseSqlServer(_connectionString).AddConceptAsSupport().Options);
}
