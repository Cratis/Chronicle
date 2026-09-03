// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.Api.EventSequences.for_EventSequenceCommands;

public class when_requesting_bulk_redaction : Specification
{
    EventSequenceCommands _commands;
    DefaultHttpContext _httpContext;
    string _responseBody;

    void Establish()
    {
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
        _commands = new EventSequenceCommands(
            Substitute.For<IEventSequences>(),
            Substitute.For<ICausationManager>())
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    async Task Because()
    {
        await _commands.RedactMany(
            "event-store",
            "namespace",
            "event-log",
            new RedactEvents("source", "reason", [], null, null));
        _httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(_httpContext.Response.Body, Encoding.UTF8);
        _responseBody = await reader.ReadToEndAsync();
    }

    [Fact] void should_return_not_implemented() => _httpContext.Response.StatusCode.ShouldEqual(StatusCodes.Status501NotImplemented);
    [Fact] void should_explain_that_bulk_redaction_is_unsupported() => _responseBody.ShouldContain("Bulk event redaction is not supported.");
}
