// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_under_a_namespace_fence;

public class and_collision_recovery_uses_the_last_attempt : given.a_fenced_append_storage
{
    void Establish()
    {
        _fenceFailure = CommandFailure(112);
        _fenceFailures = 1;
        _writeFailure = Duplicate();
        _duringWrite = () => _fenceFailures = 4;
    }

    async Task Because() => _failure = await Catch.Exception(() => _storage.AppendManyWithIdentities(_context, [_event]));

    [Fact] void should_not_start_a_fourth_transaction_for_recovery() => _attempts.ShouldEqual(3);
    [Fact] void should_preserve_the_final_definite_abort() => _failure.ShouldEqual(_fenceFailure);
    [Fact] void should_not_read_receipts_after_the_last_fence_aborts() => _lookupSessions.ShouldBeEmpty();
    [Fact] void should_not_commit_any_attempt() => _commits.ShouldEqual(0);
}
