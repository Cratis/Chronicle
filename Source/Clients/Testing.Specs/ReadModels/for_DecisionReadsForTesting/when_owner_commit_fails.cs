// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_owner_commit_fails : Specification
{
    EventStoreForTesting _store;
    Exception _commitError;
    Exception _capabilityError;
    Exception _readError;
    Exception _cleanupError;
    bool _completed;

    void Establish() => _store = new EventStoreForTesting();

    async Task Because()
    {
        var unit = (UnitOfWork)_store.UnitOfWorkManager.Begin(CorrelationId.New());
        using (var frame = unit.EnforceReadProtection())
        {
            var owner = unit.ClaimDecisionReadCommitOwnership();
            unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
            _commitError = await Catch.Exception(() => unit.CommitAsOwner(owner));
            _completed = unit.IsCompleted;
            _capabilityError = await Catch.Exception(() => frame.RunAfterCommit(log => log.CompleteStream("stream", "id")));
            _readError = await Catch.Exception(() => _store.EventLog.GetTailSequenceNumber());
        }
        _cleanupError = await Catch.Exception(() => _store.EventLog.GetTailSequenceNumber());
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_report_the_append_failure() => _commitError.ShouldBeOfExactType<UnknownEventType>();
    [Fact] void should_complete_the_failed_unit() => _completed.ShouldBeTrue();
    [Fact] void should_not_grant_completion_after_failure() => _capabilityError.ShouldBeOfExactType<ImmediateAppendWhileReadsAreProtected>();
    [Fact] void should_keep_enforcement_until_owner_cleanup() => _readError.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
    [Fact] void should_end_enforcement_at_invocation_exit() => _cleanupError.ShouldBeNull();
}
