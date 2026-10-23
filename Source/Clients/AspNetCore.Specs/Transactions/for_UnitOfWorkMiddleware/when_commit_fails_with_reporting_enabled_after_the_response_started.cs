// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_commit_fails_with_reporting_enabled_after_the_response_started : Specification
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
        _harness.Context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature(_body));
    }

    async Task Because() => await _harness.Run(context =>
    {
        _harness.AddEvent();
        return Task.CompletedTask;
    });

    [Fact] void should_not_write_problem_details() => _body.Length.ShouldEqual(0);
    [Fact] void should_not_change_the_status_code() => _harness.Context.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_log_an_error() => _harness.LogCalls(LogLevel.Error).ShouldEqual(1);

    sealed class StartedResponseFeature(Stream body) : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = body;

        public bool HasStarted => true;

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}
