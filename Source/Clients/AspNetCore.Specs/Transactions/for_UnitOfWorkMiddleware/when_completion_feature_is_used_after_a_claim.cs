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

public class when_completion_feature_is_used_after_a_claim : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    Exception _error;

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        var claim = context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        _error = await Record.ExceptionAsync(() => context.Features.Get<IUnitOfWorkCompletionFeature>()!.CommitAsync());
        await claim.Commit();
    });

    [Fact] void should_throw_already_claimed() => _error.ShouldBeOfExactType<UnitOfWorkCompletionAlreadyClaimed>();
    [Fact] void should_append_once() => _harness.AppendCalls.ShouldEqual(1);
}
