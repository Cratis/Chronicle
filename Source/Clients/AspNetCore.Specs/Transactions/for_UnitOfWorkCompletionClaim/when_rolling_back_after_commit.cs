// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkCompletionClaim;

public class when_rolling_back_after_commit : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    UnitOfWorkCompletionResult _committed;
    UnitOfWorkCompletionResult _rolledBack;

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        var claim = context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        _committed = await claim.Commit();
        _rolledBack = await claim.Rollback();
    });

    [Fact] void should_keep_the_commit_result() => ReferenceEquals(_committed, _rolledBack).ShouldBeTrue();
}
