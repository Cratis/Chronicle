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

public class when_completion_feature_commits_twice : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    Exception _error;
    IUnitOfWork _first;
    IUnitOfWork _second;

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        var feature = context.Features.Get<IUnitOfWorkCompletionFeature>()!;
        _first = await feature.CommitAsync();
        _error = await Record.ExceptionAsync(async () => _second = await feature.CommitAsync());
    });

    [Fact] void should_not_throw_the_second_time() => _error.ShouldBeNull();
    [Fact] void should_return_the_same_unit() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_append_once() => _harness.AppendCalls.ShouldEqual(1);
    [Fact] void should_not_log_the_manual_completion_warning() => _harness.LogCalls(LogLevel.Warning).ShouldEqual(0);
}
