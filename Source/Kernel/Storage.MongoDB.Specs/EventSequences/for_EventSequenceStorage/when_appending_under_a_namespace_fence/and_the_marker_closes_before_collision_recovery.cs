// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_under_a_namespace_fence;

public class and_the_marker_closes_before_collision_recovery : given.a_fenced_append_storage
{
    void Establish()
    {
        _writeFailure = Duplicate();
        _duringWrite = () => _matches = 0;
    }

    async Task Because() => _failure = await Catch.Exception(() => _storage.AppendManyWithIdentities(_context, [_event]));

    [Fact] void should_report_the_closed_marker_not_a_durable_identity() => _failure.ShouldBeOfExactType<NamespaceCommitRejected>();
    [Fact] void should_abort_both_transactions() => _steps.SequenceEqual(["fence", "insert", "abort", "fence", "abort"]).ShouldBeTrue();
    [Fact] void should_never_look_up_receipts_without_admission() => _lookupSessions.ShouldBeEmpty();
}
