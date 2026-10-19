// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkCompletionClaim;

public class when_committing_twice : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    UnitOfWorkCompletionResult _first;
    UnitOfWorkCompletionResult _second;

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        var claim = context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        _first = await claim.Commit();
        _second = await claim.Commit();
    });

    [Fact] void should_return_the_same_result() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_append_once() => _harness.AppendCalls.ShouldEqual(1);
}
