// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Chronicle.Server.for_ReservedListenSockets.when_reserving;

public class and_ipv6_is_not_supported : given.reserved_sockets
{
    Socket? _ipv6;
    Socket? _ipv4;

    void Because()
    {
        _ipv6 = _reserved.Take(new IPEndPoint(IPAddress.IPv6Any, ChroniclePort));
        _ipv4 = _reserved.Take(new IPEndPoint(IPAddress.Any, ChroniclePort));
    }

    [Fact] void should_fall_back_to_the_ipv4_any_address() => _requested.ShouldContain(new IPEndPoint(IPAddress.Any, ChroniclePort));
    [Fact] void should_not_hand_over_a_socket_for_the_ipv6_any_address() => _ipv6.ShouldBeNull();
    [Fact] void should_hand_over_the_socket_for_the_ipv4_any_address() => _ipv4.ShouldNotBeNull();

    protected override Socket CreateSocket(EndPoint endpoint) =>
        ((IPEndPoint)endpoint).Address.Equals(IPAddress.IPv6Any)
            ? throw new SocketException((int)SocketError.AddressFamilyNotSupported)
            : base.CreateSocket(endpoint);
}
