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

public class when_commit_throws_technically : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    readonly InvalidOperationException _failure = new("transport");
    UnitOfWorkCompletionResult _result;
    Exception _error;

    void Establish() => _harness.AppendThrows(_failure);

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        _error = await Record.ExceptionAsync(async () => _result = await context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim().Commit());
    });

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_be_unknown() => _result.Outcome.ShouldEqual(UnitOfWorkCompletionOutcome.Unknown);
    [Fact] void should_carry_the_exception() => _result.Exception.ShouldEqual(_failure);
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_leave_the_middleware_silent() => _harness.LogCalls(LogLevel.Error).ShouldEqual(0);
}
