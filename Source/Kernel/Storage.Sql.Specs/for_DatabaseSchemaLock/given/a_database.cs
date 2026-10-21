// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.given;

public abstract class a_database : Specification
{
    internal string ConnectionString;
    readonly List<SchemaLockDbContext> _contexts = [];

    internal abstract IDatabaseSchemaLock SchemaLock { get; }

    internal string Resource => DatabaseSchemaLock.ResourceFor(NewConnection().Database);

    internal SchemaLockDbContext NewContext()
    {
        var context = new SchemaLockDbContext(Configure(new DbContextOptionsBuilder<SchemaLockDbContext>()).Options);
        _contexts.Add(context);
        return context;
    }

    internal abstract DbConnection NewConnection();

    /// <summary>
    /// Get whether another session holds the schema lock, by trying to take it without waiting.
    /// </summary>
    /// <returns>True if it is held elsewhere.</returns>
    internal async Task<bool> IsHeldElsewhere()
    {
        await using var connection = NewConnection();
        await connection.OpenAsync();
        try
        {
            await SchemaLock.Acquire(connection, Resource, TimeSpan.Zero);
            await SchemaLock.Release(connection, Resource);
            return false;
        }
        catch (DatabaseSchemaLockNotAcquired)
        {
            return true;
        }
    }

    internal abstract DbContextOptionsBuilder<SchemaLockDbContext> Configure(DbContextOptionsBuilder<SchemaLockDbContext> builder);

    internal abstract Task CreateDatabase();

    internal abstract Task DropDatabase();

    Task Establish() => CreateDatabase();

    async Task Destroy()
    {
        foreach (var context in _contexts)
        {
            await context.DisposeAsync();
        }

        if (ConnectionString is not null)
        {
            await DropDatabase();
        }
    }
}
