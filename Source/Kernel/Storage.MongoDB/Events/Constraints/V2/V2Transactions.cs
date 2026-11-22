// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

/// <summary>Runs bounded definite-abort retries without ever replaying an ambiguous commit.</summary>
/// <param name="database">The explicitly selected namespace database.</param>
/// <param name="readConcern">Local for new-collection schema transactions, snapshot for claim mutations.</param>
internal sealed class V2Transactions(IMongoDatabase database, ReadConcern readConcern)
{
    /// <summary>Runs one atomic attempt, aborting refusals and failures before retry classification.</summary>
    /// <typeparam name="T">The operation result.</typeparam>
    /// <param name="operation">An attempt and its decision whether to commit.</param>
    /// <param name="cancellationToken">Cancellation before commit begins.</param>
    /// <param name="retryDuplicate">Whether an aborted duplicate must be resolved on a fresh snapshot.</param>
    /// <returns>The operation result.</returns>
    public async Task<T> Run<T>(Func<IClientSessionHandle, Task<(T Result, bool Commit)>> operation, CancellationToken cancellationToken, bool retryDuplicate = false)
    {
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
            session.StartTransaction(new TransactionOptions(readConcern, ReadPreference.Primary, WriteConcern.WMajority));
            try
            {
                var (result, commit) = await operation(session);
                cancellationToken.ThrowIfCancellationRequested();
                if (commit) await Commit(session);
                else await Abort(session);
                return result;
            }
            catch (MongoException error) when (CanRetry(error, retryDuplicate) && elapsed.Elapsed < TimeSpan.FromSeconds(30))
            {
                await Abort(session);
                await Task.Delay(20, cancellationToken);
            }
            catch
            {
                await Abort(session);
                throw;
            }
        }
    }

    /// <summary>Recognizes duplicate-key failures without classifying unrelated server failures as claims.</summary>
    /// <param name="error">The driver failure.</param>
    /// <returns>Whether the server reports a duplicate key.</returns>
    public static bool IsDuplicate(MongoException error) => error is MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey } || error is MongoCommandException { Code: 11000 };

    /// <summary>Recognizes only errors with a definite aborted-transaction outcome.</summary>
    /// <param name="error">The driver failure.</param>
    /// <param name="retryDuplicate">Whether this operation resolves duplicate races by rereading.</param>
    /// <returns>Whether a fresh complete attempt is safe.</returns>
    public static bool CanRetry(MongoException error, bool retryDuplicate = false)
    {
        if (error.HasErrorLabel("UnknownTransactionCommitResult")) return false;
        if (error is MongoCommandException { Code: 48 }) return false;
        if (IsDuplicate(error)) return retryDuplicate;
        if (error.HasErrorLabel("TransientTransactionError")) return true;
        if (error is MongoCommandException { Code: 112 or 244 or 246 or 251 or 12586 }) return true;
        return false;
    }

    static async Task Commit(IClientSessionHandle session)
    {
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                // Once commit begins, caller cancellation cannot introduce an ambiguous outcome.
                await session.CommitTransactionAsync(CancellationToken.None);
                return;
            }
            catch (MongoException error) when (error.HasErrorLabel("UnknownTransactionCommitResult") && elapsed.Elapsed < TimeSpan.FromSeconds(30))
            {
                // Retry this same commit, never the mutation. A remaining ambiguity is propagated.
                await Task.Delay(20, CancellationToken.None);
            }
        }
    }

    static async Task Abort(IClientSessionHandle session)
    {
        if (!session.IsInTransaction) return;
        try
        {
            await session.AbortTransactionAsync(CancellationToken.None);
        }
        catch (MongoCommandException error) when (error.Code == 251)
        {
            // The server already terminated this transaction; there is nothing left to abort.
        }
    }
}
