// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_a_cross_target_contribution_on_a_read_label : given.a_trusted_commit
{
    [Theory]
    [InlineData(ConcurrencyScopeOrigin.Explicit)]
    [InlineData(ConcurrencyScopeOrigin.Aggregate)]
    public async Task should_check_the_target_before_algebra_refusals(ConcurrencyScopeOrigin origin)
    {
        await Execute(() =>
        {
            Read();
            _unit.EnrolContributedRead(new(origin, "source", new(40, "source"), "store", "other-tenant", EventSequenceId.Log, "foreign read"));
            return Task.CompletedTask;
        });
        _error.ShouldBeOfExactType<ReadDependencyTargetMismatch>();
        _request.ShouldBeNull();
    }
}
