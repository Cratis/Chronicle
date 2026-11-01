// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts;

/// <summary>
/// Supplies real PostgreSQL incident storage.
/// </summary>
public class PostgreSqlAlertIncidentsHarness : SqlAlertIncidentsHarness
{
    /// <summary>
    /// Gets or sets the shared PostgreSQL fixture.
    /// </summary>
    public PostgreSqlFixture Fixture { get; set; } = null!;

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await Fixture.DropDatabase(_connectionString);
        await base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override NamespaceDbContext Context() => new(SpecDbContextOptions.Kernel<NamespaceDbContext>(_connectionString));

    /// <inheritdoc/>
    protected override Task<string> ConnectionString() => Fixture.CreateMigratedDatabase<NamespaceDbContext>();
}
