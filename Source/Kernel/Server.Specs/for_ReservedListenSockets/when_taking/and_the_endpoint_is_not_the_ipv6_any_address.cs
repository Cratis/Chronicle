// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Chronicle.Server.for_ReservedListenSockets.when_taking;

public class and_the_endpoint_is_not_the_ipv6_any_address : given.reserved_sockets
{
    Socket? _socket;

    void Because() => _socket = _reserved.Take(new IPEndPoint(IPAddress.Any, ChroniclePort));

    [Fact] void should_not_hand_over_a_socket() => _socket.ShouldBeNull();
}
