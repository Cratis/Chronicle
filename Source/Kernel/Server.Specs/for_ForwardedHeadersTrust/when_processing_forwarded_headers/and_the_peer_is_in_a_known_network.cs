// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Server.for_ForwardedHeadersTrust.when_processing_forwarded_headers;

public class and_the_peer_is_in_a_known_network : given.a_forwarded_request
{
    Task Because() => Process(new()
    {
        ForwardedHeaders = new() { KnownNetworks = ["192.0.2.0/24"] }
    });

    [Fact] void should_use_the_forwarded_https_scheme() => _context.Request.Scheme.ShouldEqual("https");
    [Fact] void should_use_the_forwarded_client_ip() => _context.Connection.RemoteIpAddress.ShouldEqual(IPAddress.Parse("198.51.100.12"));
}
