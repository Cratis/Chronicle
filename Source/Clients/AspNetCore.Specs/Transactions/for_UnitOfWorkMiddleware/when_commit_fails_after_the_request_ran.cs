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

public class when_commit_fails_after_the_request_ran : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    Exception _error;

    void Establish() => _harness.AppendReturns(AppendManyResult.Failed(_harness.CorrelationId, [new ConcurrencyViolation("source", 4, 5)]));

    async Task Because()
    {
        _harness.Context.Response.Body = new MemoryStream();
        _error = await Record.ExceptionAsync(() => _harness.Run(context =>
        {
            _harness.AddEvent();
            return Task.CompletedTask;
        }));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_log_an_error() => _harness.LogCalls(LogLevel.Error).ShouldEqual(1);
    [Fact] void should_not_change_the_response_by_default() => _harness.Context.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
}
