// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.for_ForwardedHeadersTrust.given;

public class a_forwarded_request : Specification
{
    protected DefaultHttpContext _context;

    void Establish()
    {
        _context = new DefaultHttpContext();
        _context.Request.Scheme = "http";
        _context.Request.Headers["X-Forwarded-Proto"] = "https";
        _context.Request.Headers["X-Forwarded-For"] = "198.51.100.12";
        _context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
    }

    protected Task Process(Configuration.ChronicleOptions options) =>
        new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(ForwardedHeadersTrust.Create(options)))
        .Invoke(_context);
}
