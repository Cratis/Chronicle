// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting.given;

public class materialized_reactors_on_a_lifecycle : Specification
{
    protected readonly List<Subject<IEnumerable<Item>>> _streams = [];
    protected readonly List<IObserver<IEnumerable<Item>>> _observers = [];
    protected readonly List<(Item Model, ReadModelChangeType ChangeType)> _changes = [];
    protected readonly Item[] _items = [new("first", "original"), new("second", "original")];
    protected IConnectionLifecycle _lifecycle;
    protected Exception? _observeFailure;
    protected ReadModelReactors _reactors;
    ServiceProvider _services;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        eventStore.Connection.Lifecycle.Returns(_lifecycle);
        var materialized = Substitute.For<IMaterializedReadModels>();
        eventStore.ReadModels.Materialized.Returns(materialized);
        materialized.ObserveInstances<Item>(Arg.Any<InstanceCountToSkip>(), Arg.Any<InstanceCount>()).Returns(_ =>
        {
            if (_observeFailure is not null) throw _observeFailure;
            var stream = new Subject<IEnumerable<Item>>();
            _streams.Add(stream);
            return new CapturedStream(stream, _observers);
        });
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.ReadModelReactors.Returns([typeof(Reactor)]);
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.Activate(Arg.Any<IServiceProvider>(), typeof(Reactor)).Returns(
            new ActivatedArtifact(new Reactor(), typeof(Reactor), Substitute.For<ILogger<ActivatedArtifact>>()));
        var invoker = Substitute.For<IReadModelReactorInvoker>();
        invoker.Invoke(eventStore, Arg.Any<object>(), Arg.Any<ReadModelReactorMethod>(), Arg.Any<object>(), Arg.Any<EventContext>(), Arg.Any<IServiceProvider>())
            .Returns(call =>
            {
                _changes.Add(((Item)call.ArgAt<object>(3), call.ArgAt<ReadModelReactorMethod>(2).ChangeType));
                return Task.CompletedTask;
            });
        _services = new ServiceCollection().BuildServiceProvider();
        _reactors = new ReadModelReactors(eventStore, artifacts, activator, _services, invoker, new JsonSerializerOptions(), Substitute.For<ILogger<ReadModelReactors>>());
        _reactors.Start();
        _streams[0].OnNext(_items);
        _changes.Clear();
    }

    protected void Reconnect()
    {
        _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
        _lifecycle.OnConnected += Raise.Event<Connected>();
        _reactors.Start();
    }

    void Destroy()
    {
        _reactors.Dispose();
        foreach (var stream in _streams) stream.Dispose();
        _services.Dispose();
    }

    sealed class CapturedStream(Subject<IEnumerable<Item>> stream, List<IObserver<IEnumerable<Item>>> observers) : IObservable<IEnumerable<Item>>
    {
        public IDisposable Subscribe(IObserver<IEnumerable<Item>> observer)
        {
            observers.Add(observer);
            return stream.Subscribe(observer);
        }
    }

    public record Item(string Id, string Name);

    [Materialized]
    public class Reactor : IReadModelReactor
    {
        public void Added(Item item) { }
        public void Modified(Item item) { }
        public void Removed(Item item) { }
    }
}
