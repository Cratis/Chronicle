// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_commit_fails_with_reporting_enabled_for_a_claiming_caller : Specification
{
    readonly UnitOfWorkHarness _harness = new();

    void Establish()
    {
        _harness.AppendReturns(AppendManyResult.Failed(_harness.CorrelationId, [new ConcurrencyViolation("source", 4, 5)]));
        var options = new ChronicleAspNetCoreOptions();
        options.UnitOfWork.ReportFailuresBeforeResponseStarts = true;
        _harness.Context.RequestServices = new ServiceCollection().AddSingleton(Microsoft.Extensions.Options.Options.Create(options)).BuildServiceProvider();
        _harness.Context.Response.Body = new MemoryStream();
    }

    async Task Because() => await _harness.Run(async context =>
    {
        _harness.AddEvent();
        await context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim().Commit();
    });

    [Fact] void should_leave_the_response_to_the_claim_owner() => _harness.Context.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_not_write_a_body() => _harness.Context.Response.Body.Length.ShouldEqual(0);
    [Fact] void should_not_log_an_error() => _harness.LogCalls(LogLevel.Error).ShouldEqual(0);
}
