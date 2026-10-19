// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkCompletionClaim;

public class when_committing_after_a_failed_direct_commit : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    readonly InvalidOperationException _failure = new("transport");
    UnitOfWorkCompletionResult _result;
    UnitOfWorkCompletionResult _repeated;
    Exception _error;

    void Establish() => _harness.AppendThrows(_failure);

    async Task Because() => await _harness.Run(async context =>
    {
        var claim = context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        _harness.AddEvent();
        _error = await Catch.Exception(claim.UnitOfWork.Commit);
        _result = await claim.Commit();
        _repeated = await claim.Commit();
    });

    [Fact] void should_propagate_the_direct_commit_exception() => _error.ShouldBeSame(_failure);
    [Fact] void should_be_unknown() => _result.Outcome.ShouldEqual(UnitOfWorkCompletionOutcome.Unknown);
    [Fact] void should_carry_the_exception() => _result.Exception.ShouldBeSame(_failure);
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_retain_the_result() => _repeated.ShouldBeSame(_result);
    [Fact] void should_not_append_again() => _harness.AppendCalls.ShouldEqual(1);
}
