// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_claimed_and_the_unit_is_committed_directly : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    UnitOfWorkCompletionResult _result;

    async Task Because() => await _harness.Run(async context =>
    {
        var claim = context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        _harness.AddEvent();
        await _harness.Unit.Commit();
        _result = await claim.Commit();
    });

    [Fact] void should_return_a_committed_result() => _result.Outcome.ShouldEqual(UnitOfWorkCompletionOutcome.Committed);
    [Fact] void should_append_once() => _harness.AppendCalls.ShouldEqual(1);
    [Fact] void should_not_log_an_error() => _harness.LogCalls(LogLevel.Error).ShouldEqual(0);
}
