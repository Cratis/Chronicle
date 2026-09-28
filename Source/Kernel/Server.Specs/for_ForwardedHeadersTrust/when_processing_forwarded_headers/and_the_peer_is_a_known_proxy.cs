// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Server.for_ForwardedHeadersTrust.when_processing_forwarded_headers;

public class and_the_peer_is_a_known_proxy : given.a_forwarded_request
{
    Task Because() => Process(new()
    {
        ForwardedHeaders = new() { KnownProxies = ["192.0.2.10"] }
    });

    [Fact] void should_use_the_forwarded_https_scheme() => _context.Request.Scheme.ShouldEqual("https");
    [Fact] void should_use_the_forwarded_client_ip() => _context.Connection.RemoteIpAddress.ShouldEqual(IPAddress.Parse("198.51.100.12"));
}
