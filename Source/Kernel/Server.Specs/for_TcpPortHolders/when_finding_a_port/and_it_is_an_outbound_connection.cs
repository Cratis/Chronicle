// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_TcpPortHolders.when_finding_a_port;

public class and_it_is_an_outbound_connection : Specification
{
    (string State, string Remote, string Inode)? _socket;

    void Because() => _socket = TcpPortHolders.Parse("  3: 0100007F:88B8 0200007F:0050 01 00000000:00000000 00:00000000 00000000 1000 0 12345 1", 35000);

    [Fact] void should_include_established_source_ports() => _socket?.State.ShouldEqual("01");
    [Fact] void should_report_the_socket_inode() => _socket?.Inode.ShouldEqual("12345");
}
