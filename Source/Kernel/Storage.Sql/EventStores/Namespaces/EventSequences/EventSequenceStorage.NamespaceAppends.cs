// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage : INamespaceFencedEventSequenceStorage
{
    /// <inheritdoc/>
    public async Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> Append(NamespaceCommitContext context, EventToAppendToStorage @event)
    {
        var result = await AppendNamespaceBatch(context, [@event], false);
        if (result.IsSuccess) return result.AsT0.Single();
        return result.AsT1.AsT0;
    }

    /// <inheritdoc/>
    public Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendMany(NamespaceCommitContext context, IEnumerable<EventToAppendToStorage> events) => AppendNamespaceBatch(context, events, false);

    /// <inheritdoc/>
    public Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendManyWithIdentities(NamespaceCommitContext context, IEnumerable<EventToAppendToStorage> events) => AppendNamespaceBatch(context, events, true);

    async Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendNamespaceBatch(NamespaceCommitContext context, IEnumerable<EventToAppendToStorage> events, bool persistIdentities)
    {
        var batch = events.ToArray();
        var appended = await PrepareNamespaceEvents(batch);
        var transaction = new NamespaceEventTransaction<EventSequenceDbContext>(OpenNamespaceEvents);
        try
        {
            return await transaction.Execute(context, async (destination, cancellationToken) =>
            {
                AddNamespaceEvents(destination, batch, persistIdentities);
                await destination.SaveChangesAsync(cancellationToken);
                return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success(appended);
            });
        }
        catch (Exception error) when (transaction.HasRemainingAttempts && StorageWriteConflicts.IsUnique(error))
        {
            var collision = await transaction.Execute(context, (destination, cancellationToken) => ResolveNamespaceCollision(destination, batch, persistIdentities, cancellationToken));
            if (collision is not null) return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Failed(collision);
            throw;
        }
    }
}
