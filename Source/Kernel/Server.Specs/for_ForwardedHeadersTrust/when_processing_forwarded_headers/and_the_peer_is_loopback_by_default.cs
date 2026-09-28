// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Server.for_ForwardedHeadersTrust.when_processing_forwarded_headers;

public class and_the_peer_is_loopback_by_default : given.a_forwarded_request
{
    void Establish() => _context.Connection.RemoteIpAddress = IPAddress.Loopback;

    Task Because() => Process(new());

    [Fact] void should_accept_forwarded_headers_from_the_loopback_proxy() => _context.Request.Scheme.ShouldEqual("https");
}
