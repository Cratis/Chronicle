// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkCompletionClaim;

public class when_committing_successfully : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    UnitOfWorkCompletionResult _result;

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        _result = await context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim().Commit();
    });

    [Fact] void should_be_committed() => _result.Outcome.ShouldEqual(UnitOfWorkCompletionOutcome.Committed);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_no_conflicts() => _result.HasConflicts.ShouldBeFalse();
    [Fact] void should_carry_the_correlation_id() => _result.CorrelationId.ShouldEqual(_harness.CorrelationId);
    [Fact] void should_append_once() => _harness.AppendCalls.ShouldEqual(1);
    [Fact] void should_not_log_an_error() => _harness.LogCalls(LogLevel.Error).ShouldEqual(0);
    [Fact] void should_not_log_a_warning() => _harness.LogCalls(LogLevel.Warning).ShouldEqual(0);
}
