// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Alerts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts;

/// <summary>
/// Supplies real SQLite incident storage with separate connections for concurrent operations.
/// </summary>
public class SqlAlertIncidentsHarness : IAlertIncidentsStorageHarness
{
    string _path;
    protected string _connectionString;

    /// <inheritdoc/>
    public async Task<IAlertIncidentsStorage> Create()
    {
        _connectionString = await ConnectionString();
        await using (var context = Context())
        {
            await context.Database.MigrateAsync();
        }
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(Context(), () => { })));

        return new AlertIncidentsStorage(EventStoreName.System, EventStoreNamespaceName.Default, database);
    }

    /// <inheritdoc/>
    public virtual ValueTask DisposeAsync()
    {
        if (_path is not null) File.Delete(_path);
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Creates a separate namespace context using the provider under specification.
    /// </summary>
    /// <returns>The namespace context.</returns>
    protected virtual NamespaceDbContext Context() => new(new DbContextOptionsBuilder<NamespaceDbContext>()
        .UseSqlite(_connectionString).AddConceptAsSupport().Options);

    /// <summary>
    /// Creates an isolated provider database.
    /// </summary>
    /// <returns>The provider connection string.</returns>
    protected virtual Task<string> ConnectionString()
    {
        _path = Path.Combine(Path.GetTempPath(), $"chronicle-alert-incidents-{Guid.NewGuid():N}.db");

        return Task.FromResult($"DataSource={_path};Pooling=False;Default Timeout=30");
    }
}
