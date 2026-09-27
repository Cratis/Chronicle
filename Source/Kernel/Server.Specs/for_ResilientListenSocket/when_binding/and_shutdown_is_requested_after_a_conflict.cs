// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.for_ResilientListenSocket.when_binding;

public class and_shutdown_is_requested_after_a_conflict : Specification
{
    readonly CancellationTokenSource _shutdown = new();
    int _attempts;
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ResilientListenSocket.Bind(
        new IPEndPoint(IPAddress.IPv6Any, 35000),
        TimeSpan.FromSeconds(30),
        NullLogger<Kernel>.Instance,
        _ =>
        {
            _attempts++;
            _shutdown.Cancel();
            throw new SocketException((int)SocketError.AddressAlreadyInUse);
        },
        _shutdown.Token));

    [Fact] void should_stop_retrying() => _attempts.ShouldEqual(1);
    [Fact] void should_preserve_the_bind_failure() => (_exception as SocketException)?.SocketErrorCode.ShouldEqual(SocketError.AddressAlreadyInUse);

    void Destroy() => _shutdown.Dispose();
}
