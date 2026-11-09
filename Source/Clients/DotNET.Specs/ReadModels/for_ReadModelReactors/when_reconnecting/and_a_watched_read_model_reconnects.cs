// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_a_watched_read_model_reconnects : Specification
{
    readonly Subject<ReadModelChangeset<Item>> _changes = new();
    IEventStore _eventStore;
    IConnectionLifecycle _lifecycle;
    ReadModelReactors _reactors;
    ServiceProvider _services;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _eventStore.Connection.Lifecycle.Returns(_lifecycle);
        _eventStore.ReadModels.Watch<Item>().Returns(_changes);
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.ReadModelReactors.Returns([typeof(Reactor)]);
        _services = new ServiceCollection().BuildServiceProvider();
        _reactors = new ReadModelReactors(_eventStore, artifacts, Substitute.For<IClientArtifactsActivator>(), _services, Substitute.For<IReadModelReactorInvoker>(), new JsonSerializerOptions(), NullLogger<ReadModelReactors>.Instance);
        _reactors.Start();
    }

    void Because()
    {
        _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
        _lifecycle.OnConnected += Raise.Event<Connected>();
        _reactors.Start();
    }

    [Fact] void should_leave_reconnection_to_the_watcher() => _eventStore.ReadModels.Received(1).Watch<Item>();
    [Fact] void should_keep_the_watched_subscription() => _changes.HasObservers.ShouldBeTrue();

    void Destroy()
    {
        _reactors.Dispose();
        _changes.Dispose();
        _services.Dispose();
    }

    public record Item(string Id);

    public class Reactor : IReadModelReactor
    {
        public void Added(Item item) { }
    }
}
