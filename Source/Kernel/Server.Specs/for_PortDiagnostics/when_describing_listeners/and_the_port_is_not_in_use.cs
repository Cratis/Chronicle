// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Chronicle.Server.for_PortDiagnostics.when_describing_listeners;

public class and_the_port_is_not_in_use : Specification
{
    int _port;
    IReadOnlyList<string> _result;

    void Establish()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        _port = ((IPEndPoint)probe.LocalEndpoint).Port;
    }

    void Because() => _result = PortDiagnostics.DescribeListeners(_port);

    [Fact]
    void should_report_no_holder()
    {
        // The diagnostic only runs on Linux (it reads /proc), so this only has something to assert there.
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        _result.ShouldContainOnly($"port {_port} is not listening inside this container at the moment of failure.");
    }
}
