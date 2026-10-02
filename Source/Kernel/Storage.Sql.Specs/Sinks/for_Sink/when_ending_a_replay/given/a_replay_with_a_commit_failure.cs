// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Globalization;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay.given;

public abstract class a_replay_with_a_commit_failure<THarness> : Contract.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    protected Exception? _error;
    protected int _primaryCount;
    protected int _revertCount;
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
        _failure = new commit_failure(FailAfterCommit);
        return CreateHarnessWithInterceptor(_failure);
    }

    protected async Task PromoteReplay()
    {
        _error = await Catch.Exception(() => _sink.EndReplay(ReplayContext()));
        _primaryCount = await CurrentCount();
        var revert = await _sink.GetInstances(ReplayContext().RevertContainerName);
        _revertCount = Convert.ToInt32(((IDictionary<string, object?>)revert.Instances.Single())["count"], CultureInfo.InvariantCulture);
    }

    sealed class commit_failure(bool afterCommit) : DbTransactionInterceptor
    {
        public bool Armed { get; set; }
        public int Attempts { get; private set; }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Armed)
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

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Armed && afterCommit && Attempts == 1)
            {
                // The server committed, but the caller did not receive its acknowledgment.
                throw new TimeoutException("Simulated lost replay commit acknowledgment");
            }

            return Task.CompletedTask;
        }
    }
}
