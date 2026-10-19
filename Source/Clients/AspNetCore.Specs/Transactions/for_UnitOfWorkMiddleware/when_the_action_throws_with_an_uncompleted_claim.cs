// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_the_action_throws_with_an_uncompleted_claim : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    readonly InvalidOperationException _actionError = new("Action failed");
    Exception _middlewareError;

    async Task Because() => _middlewareError = await Record.ExceptionAsync(() => _harness.Run(context =>
    {
        _harness.AddEvent();
        context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        throw _actionError;
    }));

    [Fact] void should_preserve_the_action_error() => _middlewareError.ShouldEqual(_actionError);
    [Fact] void should_not_commit() => _harness.AppendCalls.ShouldEqual(0);
    [Fact] void should_roll_back_the_unit() => _harness.Unit.IsCompleted.ShouldBeTrue();
    [Fact] void should_log_an_error() => _harness.LogCalls(LogLevel.Error).ShouldEqual(1);
}
