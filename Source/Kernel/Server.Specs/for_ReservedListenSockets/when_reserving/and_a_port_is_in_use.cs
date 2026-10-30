// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Chronicle.Server.for_ReservedListenSockets.when_reserving;

public class and_a_port_is_in_use : given.reserved_sockets
{
    Socket? _chronicle;
    Socket? _health;

    void Because()
    {
        _chronicle = _reserved.Take(new IPEndPoint(IPAddress.IPv6Any, ChroniclePort));
        _health = _reserved.Take(new IPEndPoint(IPAddress.IPv6Any, HealthPort));
    }

    [Fact] void should_leave_the_port_in_use_to_the_regular_bind() => _chronicle.ShouldBeNull();
    [Fact] void should_still_reserve_the_other_port() => _health.ShouldNotBeNull();

    protected override Socket CreateSocket(EndPoint endpoint) =>
        ((IPEndPoint)endpoint).Port == ChroniclePort
            ? throw new SocketException((int)SocketError.AddressAlreadyInUse)
            : base.CreateSocket(endpoint);
}
