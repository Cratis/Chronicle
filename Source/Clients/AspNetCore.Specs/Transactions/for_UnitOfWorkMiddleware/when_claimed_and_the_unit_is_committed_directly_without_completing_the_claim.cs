// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_claimed_and_the_unit_is_committed_directly_without_completing_the_claim : Specification
{
    readonly UnitOfWorkHarness _harness = new();

    async Task Because() => await _harness.Run(async context =>
    {
        context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        _harness.AddEvent();
        await _harness.Unit.Commit();
    });

    [Fact] void should_not_report_a_rollback() => _harness.LogCalls(LogLevel.Error).ShouldEqual(0);
    [Fact] void should_append_once() => _harness.AppendCalls.ShouldEqual(1);
}
