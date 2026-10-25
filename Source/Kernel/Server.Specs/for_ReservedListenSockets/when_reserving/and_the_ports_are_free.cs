// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Server.for_ReservedListenSockets.when_reserving;

public class and_the_ports_are_free : given.reserved_sockets
{
    [Fact] void should_bind_every_port_on_the_ipv6_any_address() => _requested.ShouldContainOnly(
        new IPEndPoint(IPAddress.IPv6Any, ChroniclePort),
        new IPEndPoint(IPAddress.IPv6Any, HealthPort));
}
