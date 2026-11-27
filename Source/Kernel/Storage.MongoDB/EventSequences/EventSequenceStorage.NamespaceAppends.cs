// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

public partial class EventSequenceStorage : INamespaceFencedEventSequenceStorage
{
    /// <inheritdoc/>
    public async Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> Append(NamespaceCommitContext context, EventToAppendToStorage @event)
    {
        var prepared = await PrepareNamespaceBatch([@event], false);
        var transaction = new NamespaceEventTransaction(database);
        try
        {
            return await transaction.Execute(context, async (session, cancellationToken) =>
            {
                await _collection.InsertOneAsync(session, prepared.Documents[0], cancellationToken: cancellationToken);
                return prepared.Appended[0];
            });
        }
        catch (MongoException error) when (transaction.HasRemainingAttempts && !error.HasErrorLabel("UnknownTransactionCommitResult") && IsReceiptUniqueViolation(error))
        {
            var collision = await transaction.Execute(context, (session, cancellationToken) => ResolveNamespaceCollision(session, prepared.Batch, false, cancellationToken));
            if (collision is { IsT0: true }) return collision.AsT0;
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendMany(NamespaceCommitContext context, IEnumerable<EventToAppendToStorage> events) => AppendNamespaceBatch(context, events, false);

    /// <inheritdoc/>
    public Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendManyWithIdentities(NamespaceCommitContext context, IEnumerable<EventToAppendToStorage> events) => AppendNamespaceBatch(context, events, true);

    async Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendNamespaceBatch(NamespaceCommitContext context, IEnumerable<EventToAppendToStorage> events, bool persistIdentities)
    {
        var prepared = await PrepareNamespaceBatch(events, persistIdentities);
        var transaction = new NamespaceEventTransaction(database);
        try
        {
            return await transaction.Execute(context, async (session, cancellationToken) =>
            {
                if (prepared.Documents.Length > 0) await _collection.InsertManyAsync(session, prepared.Documents, cancellationToken: cancellationToken);
                return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success(prepared.Appended);
            });
        }
        catch (MongoException error) when (transaction.HasRemainingAttempts && !error.HasErrorLabel("UnknownTransactionCommitResult") && IsReceiptUniqueViolation(error))
        {
            var collision = await transaction.Execute(context, (session, cancellationToken) => ResolveNamespaceCollision(session, prepared.Batch, persistIdentities, cancellationToken));
            if (collision is not null) return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Failed(collision);
            throw;
        }
    }
}
