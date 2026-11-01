// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Chronicle.for_ChronicleClient.when_getting_event_store;

/// <summary>
/// A serverless client connects lazily, on the first event store, and leaves registration to the deployment.
/// </summary>
public class and_serverless_registers_at_deployment : Specification
{
    IChronicleConnection _connection;
    IServices _services;
    ChronicleClient _client;

    void Establish()
    {
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IConnectionKeepAlive>();
        ((IConnectionKeepAlive)_connection).IsSkipped.Returns(true);
        var lifecycle = Substitute.For<IConnectionLifecycle>();
        lifecycle.IsConnected.Returns(true);
        _connection.Lifecycle.Returns(lifecycle);
        _connection.Connect().Returns(Task.CompletedTask);
        _services = Substitute.For<IServices>();
        ((IChronicleServicesAccessor)_connection).Services.Returns(_services);

        var options = ChronicleOptions.ForServerless("chronicle://localhost:35000/", "orders-functions", new Uri("https://orders.example.com"));
        options.Serverless.DeploymentGeneration = 1;
        options.ExplicitArtifacts.RegisterEventType<PlainEvent>();

        _client = new ChronicleClient(_connection, options, Substitute.For<IClientArtifactsProvider>());
    }

    async Task Because() => await _client.GetEventStore("the-store", "the-namespace");

    [Fact] void should_connect_once_when_the_event_store_is_requested() => _connection.Received(1).Connect();
    [Fact] void should_not_ensure_the_event_store() => _services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
    [Fact] void should_not_register_event_types() => _services.EventTypes.DidNotReceive().RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>());
    [Fact] void should_not_observe_reactors() => _services.Reactors.DidNotReceiveWithAnyArgs().Observe(default!);
    [Fact] void should_not_observe_reducers() => _services.Reducers.DidNotReceiveWithAnyArgs().Observe(default!);

    public record PlainEvent(string Name);
}
