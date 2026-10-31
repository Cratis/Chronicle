// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_only_a_rehydrated_aggregate : given.a_trusted_commit
{
    Task Because() => Execute(async () =>
    {
        _unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Aggregate, "source", new(40, "source", EventTypes: [_changed]), "store", "tenant", EventSequenceId.Log, "read-only aggregate"));
        await Commit();
    });

    [Fact] void should_complete_successfully() => _error.ShouldBeNull();
    [Fact] void should_not_validate_a_read_only_aggregate() => _request.ShouldBeNull();
    [Fact] void should_have_no_sent_conditions() => _unit.GetAppendConditions().ShouldBeEmpty();
}
