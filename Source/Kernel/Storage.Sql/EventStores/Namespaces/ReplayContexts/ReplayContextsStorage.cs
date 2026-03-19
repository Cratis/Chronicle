// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayContexts;

/// <summary>
/// Represents an implementation of <see cref="IReplayContextsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ReplayContextsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IReplayContextsStorage
{
    /// <inheritdoc/>
    public async Task Save(ReplayContext context)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var entry = ReplayContextEntryConverter.ToReplayContextEntry(context);
        await scope.DbContext.ReplayContexts.Upsert(entry);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<Result<ReplayContext, GetContextError>> TryGet(ReadModelIdentifier readModel)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entry = await scope.DbContext.ReplayContexts
            .FirstOrDefaultAsync(rc => rc.ReadModelIdentifier == readModel);

        if (entry is null)
        {
            return GetContextError.NotFound;
        }

        var context = ReplayContextEntryConverter.ToReplayContext(entry);
        return Result.Success<ReplayContext, GetContextError>(context);
    }

    /// <inheritdoc/>
    public async Task Remove(ReadModelIdentifier readModel)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entry = await scope.DbContext.ReplayContexts
            .FirstOrDefaultAsync(rc => rc.ReadModelIdentifier == readModel);

        if (entry is not null)
        {
            scope.DbContext.ReplayContexts.Remove(entry);
            await scope.DbContext.SaveChangesAsync();
        }
    }
}
