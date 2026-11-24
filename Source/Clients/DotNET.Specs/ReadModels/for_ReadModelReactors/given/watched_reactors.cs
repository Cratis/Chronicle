// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.given;

public class watched_reactors : Specification
{
    protected readonly Subject<ReadModelChangeset<Item>> _frames = new();
    protected readonly List<ReadModelChangeType> _changes = [];
    protected readonly List<EventContext> _contexts = [];
    protected ReadModelReactors _reactors;
    protected EventContext _context;
    ServiceProvider _services;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        eventStore.Reducers.HasFor<Item>().Returns(true);
        eventStore.ReadModels.Watch<Item>().Returns(_frames);
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.ReadModelReactors.Returns([typeof(Reactor)]);
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.Activate(Arg.Any<IServiceProvider>(), typeof(Reactor)).Returns(
            new ActivatedArtifact(new Reactor(), typeof(Reactor), Substitute.For<ILogger<ActivatedArtifact>>()));
        var invoker = Substitute.For<IReadModelReactorInvoker>();
        invoker.Invoke(eventStore, Arg.Any<object>(), Arg.Any<ReadModelReactorMethod>(), Arg.Any<object>(), Arg.Any<EventContext>(), Arg.Any<IServiceProvider>())
            .Returns(call =>
            {
                _changes.Add(call.ArgAt<ReadModelReactorMethod>(2).ChangeType);
                _contexts.Add(call.ArgAt<EventContext>(4));
                return Task.CompletedTask;
            });
        _services = new ServiceCollection().BuildServiceProvider();
        _reactors = new(eventStore, artifacts, activator, _services, invoker, new JsonSerializerOptions(), Substitute.For<ILogger<ReadModelReactors>>());
        _context = EventContext.EmptyWithEventSourceId("item");
        _reactors.Start();
    }

    protected void Emit(ReadModelChangeType type) => _frames.OnNext(new(
        EventStoreNamespaceName.Default,
        "item",
        type == ReadModelChangeType.Removed ? null : new Item("item"),
        type == ReadModelChangeType.Removed,
        type,
        _context));

    void Destroy()
    {
        _reactors.Dispose();
        _frames.Dispose();
        _services.Dispose();
    }

    public record Item(string Id);

    public class Reactor : IReadModelReactor
    {
        public void Added(Item item) { }
        public void Modified(Item item) { }
        public void Removed(Item item) { }
    }
}
