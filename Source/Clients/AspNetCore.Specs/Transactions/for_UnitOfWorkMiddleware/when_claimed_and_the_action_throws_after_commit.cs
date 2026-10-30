// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_claimed_and_the_action_throws_after_commit : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    readonly InvalidOperationException _actionError = new("Action failed");
    Exception _middlewareError;

    async Task Because() => _middlewareError = await Record.ExceptionAsync(() => _harness.Run(async context =>
    {
        _harness.AddEvent();
        await context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim().Commit();
        throw _actionError;
    }));

    [Fact] void should_preserve_the_action_error() => _middlewareError.ShouldEqual(_actionError);
    [Fact] void should_append_once() => _harness.AppendCalls.ShouldEqual(1);
    [Fact] void should_not_log_an_error() => _harness.LogCalls(LogLevel.Error).ShouldEqual(0);
}
