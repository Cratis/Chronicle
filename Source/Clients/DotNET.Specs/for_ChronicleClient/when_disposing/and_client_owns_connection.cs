// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_ChronicleClient.when_disposing;

public class and_client_owns_connection : Specification
{
    ChronicleClient _client;
    CancellationToken _connectionCancellationToken;

    void Establish()
    {
        _client = new ChronicleClient(new ChronicleOptions
        {
            AutoDiscoverAndRegister = false
        });
        _connectionCancellationToken = GetConnectionCancellationToken(_client);
    }

    void Because() => _client.Dispose();

    [Fact] void should_cancel_connection_token() => _connectionCancellationToken.IsCancellationRequested.ShouldBeTrue();

    static CancellationToken GetConnectionCancellationToken(ChronicleClient client)
    {
        var connectionField = typeof(ChronicleClient).GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic);
        connectionField.ShouldNotBeNull();

        var connection = connectionField.GetValue(client) as ChronicleConnection;
        connection.ShouldNotBeNull();

        var cancellationTokenField = typeof(ChronicleConnection).GetField("_cancellationToken", BindingFlags.Instance | BindingFlags.NonPublic);
        cancellationTokenField.ShouldNotBeNull();

        var cancellationToken = cancellationTokenField.GetValue(connection);
        cancellationToken.ShouldNotBeNull();
        return (CancellationToken)cancellationToken;
    }
}
