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

public class when_commit_fails_with_reporting_enabled_before_the_response_started : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    MemoryStream _body;

    void Establish()
    {
        _harness.AppendReturns(AppendManyResult.Failed(_harness.CorrelationId, [new ConcurrencyViolation("source", 4, 5)]));
        var options = new ChronicleAspNetCoreOptions();
        options.UnitOfWork.ReportFailuresBeforeResponseStarts = true;
        _harness.Context.RequestServices = new ServiceCollection().AddSingleton(Microsoft.Extensions.Options.Options.Create(options)).BuildServiceProvider();
        _body = new MemoryStream();
        _harness.Context.Response.Body = _body;
    }

    async Task Because() => await _harness.Run(context =>
    {
        _harness.AddEvent();
        return Task.CompletedTask;
    });

    [Fact] void should_respond_with_conflict() => _harness.Context.Response.StatusCode.ShouldEqual(StatusCodes.Status409Conflict);
    [Fact] void should_respond_with_problem_details() => _harness.Context.Response.ContentType.ShouldEqual("application/problem+json");
    [Fact] void should_write_the_correlation_id() => JsonDocument.Parse(_body.ToArray()).RootElement.GetProperty("correlationId").GetString().ShouldEqual(_harness.CorrelationId.ToString());
    [Fact] void should_still_log_an_error() => _harness.LogCalls(LogLevel.Error).ShouldEqual(1);
}
