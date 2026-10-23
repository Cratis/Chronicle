// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents an implementation of <see cref="IFencedBatchExecutor"/>.
/// </summary>
/// <param name="store">The <see cref="IFenceStore"/> to open transactions in.</param>
/// <param name="retryPolicy">The <see cref="FenceRetryPolicy"/> bounding retries of transient transaction errors.</param>
/// <param name="probe">The <see cref="IFencedBatchProbe"/> a fault harness pauses batches through.</param>
/// <remarks>
/// A transient transaction error aborts the whole attempt, so it is started again from the start with a new batch id.
/// An unknown commit result is never retried blindly: the store resolves it first and only a batch it proved can never commit runs again.
/// </remarks>
public class FencedBatchExecutor(IFenceStore store, FenceRetryPolicy retryPolicy, IFencedBatchProbe probe) : IFencedBatchExecutor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FencedBatchExecutor"/> class with the default retry policy and no probe.
    /// </summary>
    /// <param name="store">The <see cref="IFenceStore"/> to open transactions in.</param>
    public FencedBatchExecutor(IFenceStore store)
        : this(store, FenceRetryPolicy.Default, NullFencedBatchProbe.Instance)
    {
    }

    /// <inheritdoc/>
    public async Task<T> Execute<T>(FenceToken token, Func<IClientSessionHandle, Task<T>> writes, CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        for (var attempt = 1; ; attempt++)
        {
            var (outcome, result, error) = await Attempt(token, writes, cancellationToken);
            switch (outcome)
            {
                case FencedBatchAttemptOutcome.Committed:
                    return result!;
                case FencedBatchAttemptOutcome.Revoked:
                    throw new FenceRevoked(token);
            }

            if (attempt >= retryPolicy.MaxAttempts || DateTimeOffset.UtcNow - started >= retryPolicy.Deadline)
            {
                throw new FencedBatchRetriesExhausted(token, attempt, error!);
            }

            await Task.Delay(retryPolicy.BackoffFor(attempt), cancellationToken);
        }
    }

    async Task<(FencedBatchAttemptOutcome Outcome, T? Result, Exception? Error)> Attempt<T>(FenceToken token, Func<IClientSessionHandle, Task<T>> writes, CancellationToken cancellationToken)
    {
        var batchId = FenceBatchId.New();
        using var transaction = await store.Begin(cancellationToken);
        T result;
        try
        {
            await probe.BeforeAdmission(batchId);
            if (!await transaction.Admit(token, batchId, cancellationToken))
            {
                await transaction.Abort();
                return (FencedBatchAttemptOutcome.Revoked, default, null);
            }

            await probe.AfterAdmission(batchId);
            result = await writes(transaction.Session);
            await transaction.WriteReceipt(token, batchId, cancellationToken);
            await probe.BeforeCommit(batchId);
        }
        catch (Exception error)
        {
            await transaction.Abort();
            if (FenceRetryPolicy.IsTransient(error))
            {
                return (FencedBatchAttemptOutcome.Retry, default, error);
            }

            throw;
        }

        return await CommitOrResolve(token, batchId, transaction, result, cancellationToken);
    }

    async Task<(FencedBatchAttemptOutcome Outcome, T? Result, Exception? Error)> CommitOrResolve<T>(FenceToken token, FenceBatchId batchId, IFenceTransaction transaction, T result, CancellationToken cancellationToken)
    {
        try
        {
            await transaction.Commit(cancellationToken);
            return (FencedBatchAttemptOutcome.Committed, result, null);
        }
        catch (Exception error) when (FenceRetryPolicy.IsUnknownCommit(error))
        {
            return await store.Resolve(token, batchId, cancellationToken) switch
            {
                UnknownCommitResolution.Committed => (FencedBatchAttemptOutcome.Committed, result, null),
                UnknownCommitResolution.Revoked => (FencedBatchAttemptOutcome.Revoked, default, null),
                _ => (FencedBatchAttemptOutcome.Retry, default, error)
            };
        }
        catch (Exception error) when (FenceRetryPolicy.IsTransient(error))
        {
            return (FencedBatchAttemptOutcome.Retry, default, error);
        }
    }
}
