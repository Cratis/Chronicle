// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkCompletionClaim;

public class when_rolling_back_an_open_claim : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    UnitOfWorkCompletionResult _result;
    UnitOfWorkCompletionResult _commitAfter;

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        var claim = context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        _result = await claim.Rollback();
        _commitAfter = await claim.Commit();
    });

    [Fact] void should_be_rolled_back() => _result.Outcome.ShouldEqual(UnitOfWorkCompletionOutcome.RolledBack);
    [Fact] void should_not_append() => _harness.AppendCalls.ShouldEqual(0);
    [Fact] void should_not_commit_afterwards() => ReferenceEquals(_commitAfter, _result).ShouldBeTrue();
    [Fact] void should_complete_the_unit() => _harness.Unit.IsCompleted.ShouldBeTrue();
}
