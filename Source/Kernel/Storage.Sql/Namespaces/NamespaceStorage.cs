// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="INamespaceStorage"/> for SQL.
/// </summary>
/// <param name="dbContext">The <see cref="NamespacesDbContext"/> to use for storage operations.</param>
public class NamespaceStorage(NamespacesDbContext dbContext) : INamespaceStorage
{
    /// <inheritdoc/>
    public async Task Create(EventStoreNamespaceName name, DateTimeOffset created)
    {
        await dbContext.Namespaces.AddAsync(new Namespace { Name = name.ToString(), Created = created });
        await dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public Task Delete(EventStoreNamespaceName name)
    {
        dbContext.Namespaces.Remove(new Namespace { Name = name.ToString() });
        return dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Ensure(EventStoreNamespaceName name)
    {
        var result = await dbContext.Namespaces.FindAsync(name.ToString());
        if (result is null)
        {
            await Create(name, DateTimeOffset.UtcNow);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NamespaceState>> GetAll() =>
        await dbContext.Namespaces.Select(ns => new NamespaceState(ns.Name, ns.Created)).ToListAsync();

    /// <inheritdoc/>
    public ISubject<IEnumerable<NamespaceState>> ObserveAll() => throw new NotImplementedException();
}
