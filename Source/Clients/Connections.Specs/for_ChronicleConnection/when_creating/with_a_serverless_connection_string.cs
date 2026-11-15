// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_creating;

public class with_a_serverless_connection_string : given.a_connection_factory
{
    void Because() => _connection = CreateConnection(new ChronicleConnectionString("chronicle://localhost:35000/?serverless=true"));

    [Fact] void should_not_resolve_the_server_address() => _resolver.DidNotReceive().Resolve(Arg.Any<ChronicleConnectionString>());
    [Fact] void should_not_mark_the_lifecycle_as_connected() => _lifecycle.DidNotReceive().Connected();
}
