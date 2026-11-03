// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Channels;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.given;

public class watched_reactors : Specification
{
    protected readonly Subject<ReadModelChangeset<Item>> _changes = new();
    protected readonly List<(ReadModelChangeType Type, EventContext Context)> _recorded = [];
    protected readonly EventContext _context = EventContext.EmptyWithEventSourceId("key") with { SequenceNumber = 42 };
    protected ReadModelReactors _reactors;
    readonly Channel<(ReadModelChangeType, EventContext)> _signals = Channel.CreateUnbounded<(ReadModelChangeType, EventContext)>();
    ServiceProvider _services;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        eventStore.Reducers.HasFor<Item>().Returns(true);
        eventStore.ReadModels.Watch<Item>().Returns(_changes);
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.ReadModelReactors.Returns([typeof(Reactor)]);
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.Activate(Arg.Any<IServiceProvider>(), typeof(Reactor)).Returns(
            new ActivatedArtifact(new Reactor(), typeof(Reactor), Substitute.For<ILogger<ActivatedArtifact>>()));
        var invoker = Substitute.For<IReadModelReactorInvoker>();
        invoker.Invoke(eventStore, Arg.Any<object>(), Arg.Any<ReadModelReactorMethod>(), Arg.Any<object>(), Arg.Any<EventContext>(), Arg.Any<IServiceProvider>())
            .Returns(call =>
            {
                _signals.Writer.TryWrite((call.ArgAt<ReadModelReactorMethod>(2).ChangeType, call.ArgAt<EventContext>(4)));
                return Task.CompletedTask;
            });
        _services = new ServiceCollection().BuildServiceProvider();
        _reactors = new(eventStore, artifacts, activator, _services, invoker, new JsonSerializerOptions(), Substitute.For<ILogger<ReadModelReactors>>());
        _reactors.Start();
    }

    protected async Task Send(ReadModelChangeType type)
    {
        _changes.OnNext(new("namespace", "key", new Item("value"), type == ReadModelChangeType.Removed, type, _context));
        _recorded.Add(await _signals.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
    }

    void Destroy()
    {
        _reactors.Dispose();
        _changes.Dispose();
        _services.Dispose();
    }

    public record Item(string Value);

    public class Reactor : IReadModelReactor
    {
        public void Added(Item item) { }
        public void Modified(Item item) { }
        public void Removed(Item item) { }
    }
}
