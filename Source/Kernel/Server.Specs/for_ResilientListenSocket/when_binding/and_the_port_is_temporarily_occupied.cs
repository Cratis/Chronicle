// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.for_ResilientListenSocket.when_binding;

public class and_the_port_is_temporarily_occupied : Specification
{
    int _attempts;
    Socket _socket;

    void Because() => _socket = ResilientListenSocket.Bind(
        new IPEndPoint(IPAddress.IPv6Any, 0),
        TimeSpan.FromSeconds(2),
        NullLogger<Kernel>.Instance,
        _ =>
        {
            if (++_attempts == 1)
            {
                throw new SocketException((int)SocketError.AddressAlreadyInUse);
            }

            return SocketTransportOptions.CreateDefaultBoundListenSocket(new IPEndPoint(IPAddress.IPv6Any, 0));
        },
        CancellationToken.None);

    [Fact] void should_retry_the_bind() => _attempts.ShouldEqual(2);
    [Fact] void should_return_the_bound_socket() => _socket.IsBound.ShouldBeTrue();

    void Destroy() => _socket?.Dispose();
}
