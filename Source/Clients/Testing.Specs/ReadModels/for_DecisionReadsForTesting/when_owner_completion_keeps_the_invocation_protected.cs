// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_owner_completion_keeps_the_invocation_protected : Specification
{
    EventStoreForTesting _store;
    Exception _readError;
    Exception _appendError;
    Exception _foreignError;
    Exception _expiredError;
    bool _streamClosed;
    bool _scopeClosed;

    void Establish() => _store = new EventStoreForTesting();

    async Task Because()
    {
        var unit = (UnitOfWork)_store.UnitOfWorkManager.Begin(CorrelationId.New());
        using (var frame = unit.EnforceReadProtection())
        {
            var owner = unit.ClaimDecisionReadCommitOwnership();
            await unit.CommitAsOwner(owner);
            await frame.RunAfterCommit(async log =>
            {
                await log.CompleteStream("stream", "id");
                await log.CompleteStream(ClosedStreamScope.ForEventSource("closed-source"));
                _readError = await Catch.Exception(() => log.GetTailSequenceNumber());
                _appendError = await Catch.Exception(() => log.Append("source", new ModuleCreated("escape")));
                _foreignError = await Catch.Exception(() => _store.GetEventSequence("foreign").CompleteStream("stream", "id"));
            });
            _expiredError = await Catch.Exception(() => _store.EventLog.CompleteStream("other-stream", "id"));
        }
        _streamClosed = await _store.EventLog.IsStreamCompleted("stream", "id");
        _scopeClosed = await _store.EventLog.IsStreamCompleted(ClosedStreamScope.ForEventSource("closed-source"));
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_complete_the_stream_after_owner_commit() => _streamClosed.ShouldBeTrue();
    [Fact] void should_complete_the_scope_after_owner_commit() => _scopeClosed.ShouldBeTrue();
    [Fact] void should_not_allow_raw_reads_inside_the_capability() => _readError.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
    [Fact] void should_not_allow_appends_inside_the_capability() => _appendError.ShouldBeOfExactType<ImmediateAppendWhileReadsAreProtected>();
    [Fact] void should_bind_the_capability_to_the_full_target() => _foreignError.ShouldBeOfExactType<ImmediateAppendWhileReadsAreProtected>();
    [Fact] void should_expire_the_capability_after_the_callback() => _expiredError.ShouldBeOfExactType<ImmediateAppendWhileReadsAreProtected>();
}
