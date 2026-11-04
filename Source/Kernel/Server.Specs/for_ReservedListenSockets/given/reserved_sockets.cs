// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

namespace Cratis.Chronicle.Server.for_ReservedListenSockets.given;

public class reserved_sockets : Specification
{
    protected const int ChroniclePort = 35000;
    protected const int HealthPort = 8081;
    protected readonly List<EndPoint> _requested = [];
    protected readonly List<Socket> _created = [];
    internal ReservedListenSockets _reserved;

    void Establish() => _reserved = ReservedListenSockets.Reserve([ChroniclePort, HealthPort], CreateSocket);

    void Destroy()
    {
        _reserved.Dispose();
        _created.ForEach(_ => _.Dispose());
    }

    protected virtual Socket CreateSocket(EndPoint endpoint)
    {
        _requested.Add(endpoint);
        var socket = SocketTransportOptions.CreateDefaultBoundListenSocket(new IPEndPoint(IPAddress.IPv6Any, 0));
        _created.Add(socket);
        return socket;
    }
}
