// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_resolving_the_servers_fails_for_another_reason : given.a_connection_factory
{
    readonly InvalidOperationException _failure = new("misconfigured");
    Exception _error;

    void Establish()
    {
        _connection = CreateConnection(new ChronicleConnectionString("chronicle+srv://chronicle.example.local"));
        _resolver
            .Resolve(Arg.Any<ChronicleConnectionString>())
            .Returns(Task.FromException<IReadOnlyList<ChronicleServerAddress>>(_failure));
    }

    async Task Because() => _error = await Catch.Exception(_connection.Connect);

    [Fact] void should_surface_the_failure() => _error.ShouldEqual(_failure);
}
