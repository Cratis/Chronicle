// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Wraps the real store so the first batch's commit reports an unknown result, after doing what the mode says on the real server.
/// </summary>
/// <param name="inner">The real <see cref="IFenceStore"/>.</param>
/// <param name="mode">What really happens to the first transaction.</param>
/// <param name="betweenCommitAndReply">Runs after the real outcome and before the unknown result is reported.</param>
public class UnknownCommitStore(IFenceStore inner, UnknownCommitMode mode, Func<Task> betweenCommitAndReply) : IFenceStore
{
    int _begun;

    public int Resolutions { get; private set; }

    public async Task<IFenceTransaction> Begin(CancellationToken cancellationToken)
    {
        var transaction = await inner.Begin(cancellationToken);
        return Interlocked.Increment(ref _begun) == 1 ? new Transaction(transaction, mode, betweenCommitAndReply) : transaction;
    }

    public Task<UnknownCommitResolution> Resolve(FenceToken token, FenceBatchId batchId, CancellationToken cancellationToken)
    {
        Resolutions++;
        return inner.Resolve(token, batchId, cancellationToken);
    }

    sealed class Transaction(IFenceTransaction inner, UnknownCommitMode mode, Func<Task> between) : IFenceTransaction
    {
        public global::MongoDB.Driver.IClientSessionHandle Session => inner.Session;

        public Task<bool> Admit(FenceToken token, FenceBatchId batchId, CancellationToken cancellationToken) => inner.Admit(token, batchId, cancellationToken);

        public Task WriteReceipt(FenceToken token, FenceBatchId batchId, CancellationToken cancellationToken) => inner.WriteReceipt(token, batchId, cancellationToken);

        public async Task Commit(CancellationToken cancellationToken)
        {
            switch (mode)
            {
                case UnknownCommitMode.CommittedButReplyLost:
                    await inner.Commit(cancellationToken);
                    break;
                case UnknownCommitMode.AbortedButReplyLost:
                    await inner.Abort();
                    break;
            }

            await between();
            var error = new global::MongoDB.Driver.MongoException("commit reply lost");
            error.AddErrorLabel(FenceRetryPolicy.UnknownTransactionCommitResult);
            throw error;
        }

        public Task Abort() => mode == UnknownCommitMode.LeftOpenAndReplyLost ? Task.CompletedTask : inner.Abort();

        public void Dispose()
        {
            // A transaction left open must not be ended by disposing its session; it stays until the server reaps it.
            if (mode != UnknownCommitMode.LeftOpenAndReplyLost)
            {
                inner.Dispose();
            }
        }
    }
}
