// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkCompletionClaim;

public class when_the_unit_is_committed_directly_and_throws : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    readonly InvalidOperationException _failure = new("transport");
    UnitOfWorkCompletionResult _committed;
    UnitOfWorkCompletionResult _rolledBack;

    void Establish() => _harness.AppendThrows(_failure);

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        var claim = context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        await Record.ExceptionAsync(() => claim.UnitOfWork.Commit());
        _committed = await claim.Commit();
        _rolledBack = await claim.Rollback();
    });

    [Fact] void should_report_the_commit_as_unknown() => _committed.Outcome.ShouldEqual(UnitOfWorkCompletionOutcome.Unknown);
    [Fact] void should_carry_the_exception() => _committed.Exception.ShouldEqual(_failure);
    [Fact] void should_not_report_a_rollback() => _rolledBack.Outcome.ShouldEqual(UnitOfWorkCompletionOutcome.Unknown);
}
