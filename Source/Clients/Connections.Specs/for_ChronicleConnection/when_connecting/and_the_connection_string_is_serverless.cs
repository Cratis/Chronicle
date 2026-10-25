// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_the_connection_string_is_serverless : given.a_connection_factory
{
    void Establish() => _connection = CreateConnection(new ChronicleConnectionString("chronicle://localhost:35000/?serverless=true"));

    Task Because() => _connection.Connect();

    [Fact] void should_report_keep_alive_as_skipped() => ((IConnectionKeepAlive)_connection).IsSkipped.ShouldBeTrue();
    [Fact] void should_mark_the_lifecycle_as_connected() => _lifecycle.Received(1).Connected();
}
