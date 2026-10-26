// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_a_constraint_fails_with_reporting_enabled : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    readonly MemoryStream _body = new();
    HttpContext _context;
    JsonDocument _problem;

    void Establish()
    {
        var options = new ChronicleAspNetCoreOptions();
        options.UnitOfWork.ReportFailuresBeforeResponseStarts = true;
        _harness.Context.RequestServices = new ServiceCollection().AddSingleton(Microsoft.Extensions.Options.Options.Create(options)).BuildServiceProvider();
        _harness.AppendReturns(AppendManyResult.Failed(_harness.CorrelationId,
            [new ConstraintViolation("event", 0, ConstraintType.Unique, "unique-name", "Already used", new())]));
    }

    async Task Because()
    {
        await _harness.Run(context =>
        {
            _context = context;
            context.Response.Body = _body;
            _harness.AddEvent();
            return Task.CompletedTask;
        });
        _problem = JsonDocument.Parse(_body.ToArray());
    }

    [Fact] void should_return_bad_request() => _context.Response.StatusCode.ShouldEqual(400);
    [Fact] void should_return_problem_details() => _context.Response.ContentType.ShouldEqual("application/problem+json");
    [Fact] void should_include_the_validation_errors() => _problem.RootElement.GetProperty("errors").GetProperty("unique-name")[0].GetString().ShouldEqual("Already used");
    [Fact] void should_include_the_correlation_id() => _problem.RootElement.GetProperty("correlationId").GetString().ShouldEqual(_harness.CorrelationId.ToString());
}
