// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.for_ResilientListenSocket.when_binding;

public class and_the_port_remains_occupied : Specification
{
    int _attempts;
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ResilientListenSocket.Bind(
        new IPEndPoint(IPAddress.IPv6Any, 35000),
        TimeSpan.Zero,
        NullLogger<Kernel>.Instance,
        _ =>
        {
            _attempts++;
            throw new SocketException((int)SocketError.AddressAlreadyInUse);
        }));

    [Fact] void should_not_retry_without_a_budget() => _attempts.ShouldEqual(1);
    [Fact] void should_preserve_the_socket_error() => (_exception as SocketException)?.SocketErrorCode.ShouldEqual(SocketError.AddressAlreadyInUse);
}
