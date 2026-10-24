// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_completion_callback_fails : Specification
{
    EventStoreForTesting _store;
    Exception _callbackError;
    Exception _inheritedError;
    Exception _readError;
    Exception _cleanupError;

    void Establish() => _store = new EventStoreForTesting();

    async Task Because()
    {
        var unit = (UnitOfWork)_store.UnitOfWorkManager.Begin(CorrelationId.New());
        using (var frame = unit.EnforceReadProtection())
        {
            var owner = unit.ClaimDecisionReadCommitOwnership();
            await unit.CommitAsOwner(owner);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<Exception?> inherited = null!;
            _callbackError = await Catch.Exception(() => frame.RunAfterCommit(log =>
            {
                inherited = Task.Run(async () =>
                {
                    await release.Task;
                    return await Catch.Exception(() => log.CompleteStream("stream", "id"));
                });
                throw new OperationCanceledException("Completion canceled");
            }));
            release.SetResult();
            _inheritedError = (await inherited)!;
            _readError = await Catch.Exception(() => _store.EventLog.GetTailSequenceNumber());
        }
        _cleanupError = await Catch.Exception(() => _store.EventLog.GetTailSequenceNumber());
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_propagate_callback_cancellation() => _callbackError.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_revoke_inherited_completion_permissions() => _inheritedError.ShouldBeOfExactType<ImmediateAppendWhileReadsAreProtected>();
    [Fact] void should_keep_enforcement_until_owner_cleanup() => _readError.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
    [Fact] void should_end_enforcement_at_invocation_exit() => _cleanupError.ShouldBeNull();
}
