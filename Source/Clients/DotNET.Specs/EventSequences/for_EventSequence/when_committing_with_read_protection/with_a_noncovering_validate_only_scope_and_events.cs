// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_a_noncovering_validate_only_scope_and_events : given.a_trusted_commit
{
    Task Because() => Execute(async () =>
    {
        Read();
        _unit.AddEvents(EventSequenceId.Log, [new("source", "changed")], [new("source", new ConcurrencyScope(101, "source"))]);
        await Commit();
    });

    [Fact] void should_refuse_the_later_boundary() => _error.ShouldBeOfExactType<ExplicitScopeDoesNotCoverReads>();
    [Fact] void should_complete_the_unit() => _unit.IsCompleted.ShouldBeTrue();
    [Fact] void should_notify_the_owner() => _onCompletedCalled.ShouldBeTrue();
    [Fact] void should_record_a_failed_result() => _unit.IsSuccess.ShouldBeFalse();
    [Fact] void should_release_the_invocation_frame() => ReadProtectionFrame.Current.ShouldBeNull();
    [Fact] void should_not_send_an_append() => _request.ShouldBeNull();
}
