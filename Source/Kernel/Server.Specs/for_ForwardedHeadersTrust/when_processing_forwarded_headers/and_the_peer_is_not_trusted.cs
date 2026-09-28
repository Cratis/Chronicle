// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Server.for_ForwardedHeadersTrust.when_processing_forwarded_headers;

public class and_the_peer_is_not_trusted : given.a_forwarded_request
{
    Task Because() => Process(new());

    [Fact] void should_preserve_the_http_scheme() => _context.Request.Scheme.ShouldEqual("http");
    [Fact] void should_preserve_the_remote_ip() => _context.Connection.RemoteIpAddress.ShouldEqual(IPAddress.Parse("192.0.2.10"));
}
