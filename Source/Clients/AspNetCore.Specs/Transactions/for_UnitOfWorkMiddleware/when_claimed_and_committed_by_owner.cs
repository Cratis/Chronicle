// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_claimed_and_committed_by_owner : Specification
{
    readonly UnitOfWorkHarness _harness = new();

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        await context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim().Commit();
    });

    [Fact] void should_append_once() => _harness.AppendCalls.ShouldEqual(1);
    [Fact] void should_not_log_the_manual_completion_warning() => _harness.LogCalls(LogLevel.Warning).ShouldEqual(0);
    [Fact] void should_not_log_an_error() => _harness.LogCalls(LogLevel.Error).ShouldEqual(0);
    [Fact] void should_remove_the_ownership_feature() => _harness.Context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>().ShouldBeNull();
}
