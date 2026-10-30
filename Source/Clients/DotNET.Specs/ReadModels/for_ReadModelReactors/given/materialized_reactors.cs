// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.given;

public class materialized_reactors : Specification
{
    protected readonly Subject<IEnumerable<Item>> _instances = new();
    protected readonly List<(Item Model, ReadModelChangeType ChangeType)> _changes = [];
    protected Item[] _items;
    protected InstanceCount _take;
    ReadModelReactors _reactors;
    ServiceProvider _services;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        var materialized = Substitute.For<IMaterializedReadModels>();
        eventStore.ReadModels.Materialized.Returns(materialized);
        materialized.ObserveInstances<Item>(Arg.Any<InstanceCountToSkip>(), Arg.Any<InstanceCount>()).Returns(call =>
        {
            _take = call.ArgAt<InstanceCount>(1) ?? InstanceCount.Default;
            return _instances.Select(items => items.Take(_take.Value));
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
        _items = Enumerable.Range(1, 51).Select(index => new Item(index.ToString(), "original")).ToArray();
        _instances.OnNext(_items);
        _changes.Clear();
    }

    void Destroy()
    {
        _reactors.Dispose();
        _instances.Dispose();
        _services.Dispose();
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
