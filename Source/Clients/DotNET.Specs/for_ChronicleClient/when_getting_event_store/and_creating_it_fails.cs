// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.for_ChronicleClient.when_getting_event_store;

/// <summary>
/// A store whose creation failed is never handed to a caller, so nothing it subscribed to the shared connection may
/// outlive the failure. Otherwise every failed attempt in a long-lived client leaves one more store rooted by it.
/// </summary>
public class and_creating_it_fails : Specification
{
    RecordingConnectionLifecycle _lifecycle;
    ChronicleClient _client;
    Exception _error;
    int _baselineConnected;
    int _baselineDisconnected;
    int _baselineDisconnectedAdds;

    void Establish()
    {
        _lifecycle = new RecordingConnectionLifecycle();
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(_lifecycle);
        ((IChronicleServicesAccessor)connection).Services.Returns(Substitute.For<IServices>());

        // Connecting is the last thing constructing an event store does, so failing it fails the construction.
        connection.Connect().Returns(Task.FromException(new InvalidOperationException("the kernel was not reachable")));
        _client = new ChronicleClient(connection, new ChronicleOptions { AutoDiscoverAndRegister = false });
        _baselineConnected = _lifecycle.ConnectedCount;
        _baselineDisconnected = _lifecycle.DisconnectedCount;
        _baselineDisconnectedAdds = _lifecycle.TotalDisconnectedAdds;
    }

    async Task Because() => _error = await Catch.Exception(() => _client.GetEventStore("the-store", "the-namespace"));

    [Fact] void should_fail() => _error.ShouldNotBeNull();
    [Fact] void should_have_subscribed_while_building_the_store() => _lifecycle.TotalDisconnectedAdds.ShouldBeGreaterThan(_baselineDisconnectedAdds);
    [Fact] void should_leave_the_disconnected_subscriptions_as_they_were() => _lifecycle.DisconnectedCount.ShouldEqual(_baselineDisconnected);
    [Fact] void should_leave_the_connected_subscriptions_as_they_were() => _lifecycle.ConnectedCount.ShouldEqual(_baselineConnected);
}
