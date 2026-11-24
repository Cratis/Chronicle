// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces;

/// <summary>
/// Commits a namespace conflict write and event/receipt work on the namespace destination's own client.
/// </summary>
/// <param name="database">The resolved namespace event database.</param>
internal sealed class NamespaceEventTransaction(IEventStoreNamespaceDatabase database)
{
    const int MaximumAttempts = 3;

    static readonly TransactionOptions _options = new(
        readConcern: ReadConcern.Snapshot,
        readPreference: ReadPreference.Primary,
        writeConcern: WriteConcern.WMajority);

    readonly IMongoCollection<BsonDocument> _markers = database.GetCollection<BsonDocument>(WellKnownCollectionNames.NamespaceMarker);
    int _attempts;

    /// <summary>
    /// Gets whether this operation can start another transaction, including fenced collision recovery.
    /// </summary>
    public bool HasRemainingAttempts => _attempts < MaximumAttempts;

    /// <summary>
    /// Runs event/receipt work only after writing the exact open marker in the same transaction.
    /// </summary>
    /// <typeparam name="T">The value made available only after a successful commit.</typeparam>
    /// <param name="context">The immutable identity and activation lifetime shared by all attempts.</param>
    /// <param name="write">Work that must use the supplied session and cancellation token.</param>
    /// <returns>The committed work's result.</returns>
    /// <exception cref="NamespaceCommitRejected">The expected marker is absent, closed, or replaced.</exception>
    /// <exception cref="NamespaceCommitNotSupported">The destination cannot run transactions.</exception>
    /// <exception cref="NamespaceIncarnationRequiresReactivation">The calling activation has stale or unknown state.</exception>
    /// <exception cref="NamespaceCommitOutcomeUnknown">The commit was submitted without a definite outcome.</exception>
    /// <exception cref="InvalidOperationException">The operation's transaction attempt budget is exhausted.</exception>
    public async Task<T> Execute<T>(NamespaceCommitContext context, Func<IClientSessionHandle, CancellationToken, Task<T>> write)
    {
        context.ActivationCancellation.ThrowIfCancellationRequested();
        if (context.Incarnation.Value == Guid.Empty) throw new NamespaceCommitRejected();
        await RequireTransactions(context.ActivationCancellation);
        if (!HasRemainingAttempts) throw new InvalidOperationException("The namespace transaction attempt budget is exhausted.");
        for (_attempts++; ; _attempts++)
        {
            context.ActivationCancellation.ThrowIfCancellationRequested();
            try
            {
                return await Attempt(context, write);
            }
            catch (MongoException error) when (IsWriteConflict(error) && HasRemainingAttempts)
            {
                // Only a definite server-side write conflict can restart the transaction body.
            }
        }
    }

    static bool IsWriteConflict(MongoException error) => !error.HasErrorLabel("UnknownTransactionCommitResult") && error switch
    {
        MongoCommandException command => command.Code == 112,
        MongoWriteException write => write.WriteError.Code == 112 && write.WriteConcernError is null,
        MongoBulkWriteException<Event> bulk => bulk.WriteErrors.Any(item => item.Code == 112) && bulk.WriteConcernError is null,
        _ => false
    };

    static async Task Commit(IClientSessionHandle session, CancellationToken cancellationToken)
    {
        try
        {
            await session.CommitTransactionAsync(cancellationToken);
        }
        catch (MongoException error) when (IsWriteConflict(error))
        {
            throw;
        }
        catch (Exception error)
        {
            // Includes cancellation after dispatch: an aborted transaction cannot be inferred from it.
            throw new NamespaceCommitOutcomeUnknown(error);
        }
    }

    static async Task Abort(IClientSessionHandle session)
    {
        if (!session.IsInTransaction) return;
        try
        {
            await session.AbortTransactionAsync(CancellationToken.None);
        }
        catch (MongoException)
        {
            // No commit was submitted, or a definite write conflict already aborted it on the server.
        }
    }

    async Task RequireTransactions(CancellationToken cancellationToken)
    {
        try
        {
            await MongoTopology.RequireTransactions(_markers.Database, cancellationToken);
        }
        catch (InvalidOperationException error)
        {
            throw new NamespaceCommitNotSupported(error);
        }
    }

    async Task<T> Attempt<T>(NamespaceCommitContext context, Func<IClientSessionHandle, CancellationToken, Task<T>> write)
    {
        var cancellationToken = context.ActivationCancellation;
        using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction(_options);
        try
        {
            var marker = new NamespaceMarkerStorage(_markers);
            if (!await marker.TryFence(session, context.Incarnation, cancellationToken)) throw await marker.GetRejection(session, context.Incarnation, cancellationToken);
            if (context.ActivationIncarnation != context.Incarnation) throw new NamespaceIncarnationRequiresReactivation();
            cancellationToken.ThrowIfCancellationRequested();
            var result = await write(session, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await Commit(session, cancellationToken);
            return result;
        }
        catch (NamespaceCommitOutcomeUnknown)
        {
            // Do not abort, retry, or inspect a sessionless receipt to manufacture a known outcome.
            throw;
        }
        catch
        {
            await Abort(session);
            throw;
        }
    }
}
