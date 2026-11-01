// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_ChronicleClient.when_getting_event_store;

public class and_serverless_client_is_constructed : Specification
{
    IChronicleConnection _connection;

    void Establish()
    {
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IConnectionKeepAlive>();
        ((IConnectionKeepAlive)_connection).IsSkipped.Returns(true);
        _connection.Lifecycle.Returns(Substitute.For<IConnectionLifecycle>());
    }

    void Because() => _ = new ChronicleClient(
        _connection,
        ChronicleOptions.ForServerless("chronicle://localhost:35000/", "orders-functions", new Uri("https://orders.example.com")));

    [Fact] void should_not_connect() => _connection.DidNotReceive().Connect();
}
