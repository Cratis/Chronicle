// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Changes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Changesets;

/// <summary>
/// Represents an implementation of <see cref="IChangesetStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ChangesetStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IChangesetStorage
{
    /// <inheritdoc/>
    public async Task BeginReplay(ReadModelContainerName readModel)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var changesets = scope.DbContext.Changesets.Where(changeset => changeset.ReadModelName == readModel);
        scope.DbContext.Changesets.RemoveRange(changesets);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public Task EndReplay(ReadModelContainerName readModel) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task Save(
        ReadModelContainerName readModel,
        Key readModelKey,
        EventType eventType,
        EventSequenceNumber sequenceNumber,
        CorrelationId correlationId,
        IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var entity = ChangesetConverters.ToSql(readModel, readModelKey, eventType, sequenceNumber, correlationId, changeset);
        await scope.DbContext.Changesets.AddAsync(entity);
        await scope.DbContext.SaveChangesAsync();
    }
}
