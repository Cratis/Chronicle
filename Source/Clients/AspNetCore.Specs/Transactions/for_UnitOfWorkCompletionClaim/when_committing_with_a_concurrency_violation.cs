// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkCompletionClaim;

public class when_committing_with_a_concurrency_violation : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    UnitOfWorkCompletionResult _result;

    void Establish() => _harness.AppendReturns(AppendManyResult.Failed(_harness.CorrelationId, [new ConcurrencyViolation("source", 4, 5)]));

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        _result = await context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim().Commit();
    });

    [Fact] void should_be_failed() => _result.Outcome.ShouldEqual(UnitOfWorkCompletionOutcome.Failed);
    [Fact] void should_have_conflicts() => _result.HasConflicts.ShouldBeTrue();
    [Fact] void should_report_the_violation() => _result.ConcurrencyViolations.Count.ShouldEqual(1);
    [Fact] void should_not_throw_or_carry_an_exception() => _result.Exception.ShouldBeNull();
    [Fact] void should_leave_the_middleware_silent() => _harness.LogCalls(LogLevel.Error).ShouldEqual(0);
}
