// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Chronicle.Server.for_PortDiagnostics.when_describing_listeners;

public class and_the_port_is_held_by_this_process : Specification
{
    TcpListener _listener;
    int _port;
    IReadOnlyList<string> _result;

    void Establish()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
    }

    void Because() => _result = PortDiagnostics.DescribeListeners(_port);

    [Fact]
    void should_name_this_process_as_the_holder()
    {
        // The diagnostic only runs on Linux (it reads /proc), so this only has something to assert there.
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        _result.ShouldContain(line => line.Contains($"port {_port} is held by pid {Environment.ProcessId} (", StringComparison.Ordinal));
    }

    void Destroy() => _listener.Stop();
}
