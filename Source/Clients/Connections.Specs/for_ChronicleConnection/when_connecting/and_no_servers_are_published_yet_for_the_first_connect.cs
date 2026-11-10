// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_no_servers_are_published_yet_for_the_first_connect : given.a_connection_factory
{
    Exception _error;

    void Establish()
    {
        _connection = CreateConnection(new ChronicleConnectionString("chronicle+srv://chronicle.example.local"));
        _resolver
            .Resolve(Arg.Any<ChronicleConnectionString>())
            .Returns(Task.FromException<IReadOnlyList<ChronicleServerAddress>>(new NoSrvRecordsFound("_chronicle._tcp.chronicle.example.local")));
    }

    async Task Because() => _error = await Catch.Exception(_connection.Connect);

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] void should_have_tried_to_resolve_the_servers() => _resolver.Received().Resolve(Arg.Any<ChronicleConnectionString>());
    [Fact] void should_not_report_the_connection_as_connected() => _lifecycle.DidNotReceive().Connected();
}
