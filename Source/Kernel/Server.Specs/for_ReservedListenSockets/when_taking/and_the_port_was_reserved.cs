// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Chronicle.Server.for_ReservedListenSockets.when_taking;

public class and_the_port_was_reserved : given.reserved_sockets
{
    Socket? _first;
    Socket? _second;

    void Because()
    {
        _first = _reserved.Take(new IPEndPoint(IPAddress.IPv6Any, ChroniclePort));
        _second = _reserved.Take(new IPEndPoint(IPAddress.IPv6Any, ChroniclePort));
    }

    [Fact] void should_hand_over_the_reserved_socket() => _first.ShouldEqual(_created[0]);
    [Fact] void should_hand_it_over_only_once() => _second.ShouldBeNull();
}
