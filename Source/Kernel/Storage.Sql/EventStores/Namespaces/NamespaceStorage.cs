// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="INamespaceStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class NamespaceStorage(EventStoreName eventStore, IDatabase database) : INamespaceStorage
{
    /// <inheritdoc/>
    public async Task Create(EventStoreNamespaceName name, DateTimeOffset created)
    {
        await using var scope = await database.EventStore(eventStore);
        await scope.DbContext.Namespaces.AddAsync(new Namespace { Name = name.ToString(), Created = created });
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Delete(EventStoreNamespaceName name)
    {
        await using var scope = await database.EventStore(eventStore);
        scope.DbContext.Namespaces.Remove(new Namespace { Name = name.ToString() });
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Ensure(EventStoreNamespaceName name)
    {
        await using var scope = await database.EventStore(eventStore);
        var result = await scope.DbContext.Namespaces.FindAsync(name.ToString());
        if (result is null)
        {
            await Create(name, DateTimeOffset.UtcNow);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NamespaceState>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.Namespaces.Select(ns => new NamespaceState(ns.Name, ns.Created)).ToListAsync();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<NamespaceState>> ObserveAll() => throw new NotImplementedException();
}
