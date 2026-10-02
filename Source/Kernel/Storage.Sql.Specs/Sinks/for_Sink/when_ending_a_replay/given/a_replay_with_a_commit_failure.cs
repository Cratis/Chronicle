// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Globalization;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay.given;

public abstract class a_replay_with_a_commit_failure<THarness> : Contract.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    protected Exception? _error;
    protected int? _primaryCount;
    protected int? _revertCount;
    protected abstract bool FailAfterCommit { get; }
    protected int CommitAttempts => _failure.Attempts;

    commit_failure _failure;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
        _failure.Armed = true;
    }

    protected abstract THarness CreateHarnessWithInterceptor(IInterceptor interceptor);

    protected override THarness CreateHarness()
    {
        _failure = new commit_failure(FailAfterCommit, AfterCommit, BeforeRetry);
        return CreateHarnessWithInterceptor(_failure);
    }

    protected virtual Task AfterCommit() => Task.CompletedTask;

    protected virtual Task BeforeRetry() => Task.CompletedTask;

    protected async Task PromoteReplay()
    {
        _error = await Catch.Exception(() => _sink.EndReplay(ReplayContext()));
        _primaryCount = await CurrentCountOrNull();
        var revert = await _sink.GetInstances(ReplayContext().RevertContainerName);
        var previous = revert.Instances.SingleOrDefault();
        _revertCount = previous is null ? null : Convert.ToInt32(((IDictionary<string, object?>)previous)["count"], CultureInfo.InvariantCulture);
    }

    sealed class commit_failure(bool afterCommit, Func<Task> afterFirstCommit, Func<Task> beforeRetry) : DbTransactionInterceptor
    {
        DbContext? _promotionContext;
        int _starts;

        public bool Armed { get; set; }
        public int Attempts { get; private set; }

        public override async ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
        {
            if (Armed)
            {
                _promotionContext ??= eventData.Context;
                if (eventData.Context == _promotionContext && ++_starts > 1)
                {
                    await beforeRetry().WaitAsync(TimeSpan.FromSeconds(10));
                }
            }

            return result;
        }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Armed && eventData.Context == _promotionContext)
            {
                Attempts++;
                if (!afterCommit && Attempts == 1)
                {
                    // A provider-recognized transient failure before commit must retry the rolled-back swap.
                    throw new TimeoutException("Simulated failure before replay commit");
                }
            }

            return ValueTask.FromResult(result);
        }

        public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Armed && eventData.Context == _promotionContext && afterCommit && Attempts == 1)
            {
                // Interleave another scope after the server commits, before EF can verify the outcome.
                await afterFirstCommit().WaitAsync(TimeSpan.FromSeconds(10));
                throw new TimeoutException("Simulated lost replay commit acknowledgment");
            }
        }
    }
}
