// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Kernel.Integration.Storage.Sql.Alerts;

/// <summary>
/// Supplies real SQL Server execution of the shared incident storage contract.
/// </summary>
public class SqlServerAlertIncidentsHarness : SqlAlertIncidentsHarness
{
    /// <summary>
    /// Gets or sets the SQL Server fixture.
    /// </summary>
    public SqlServerFixture Fixture { get; set; } = null!;

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await Fixture.DropDatabase(_connectionString);
        await base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override NamespaceDbContext Context() => new(new DbContextOptionsBuilder<NamespaceDbContext>()
        .UseSqlServer(_connectionString).AddConceptAsSupport().Options);

    /// <inheritdoc/>
    protected override Task<string> ConnectionString() => Fixture.CreateDatabase();
}
