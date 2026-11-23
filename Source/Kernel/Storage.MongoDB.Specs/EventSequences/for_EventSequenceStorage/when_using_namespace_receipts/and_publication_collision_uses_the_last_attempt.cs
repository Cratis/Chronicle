// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_publication_collision_uses_the_last_attempt : given.a_fenced_receipt_storage
{
    void Establish()
    {
        _fenceFailure = CommandFailure(112);
        _fenceFailures = 1;
        _writeFailure = Duplicate();
        _duringWrite = () => _fenceFailures = 4;
    }

    async Task Because() => _failure = await Catch.Exception(() => _storage.AppendPublication(_context, _publication, _event));

    [Fact] void should_not_start_a_fourth_transaction() => _attempts.ShouldEqual(3);
    [Fact] void should_preserve_the_final_abort() => _failure.ShouldEqual(_fenceFailure);
    [Fact] void should_not_read_publications_after_the_final_fence_aborts() => _lookupSessions.Count.ShouldEqual(1);
    [Fact] void should_not_commit() => _commits.ShouldEqual(0);
}
