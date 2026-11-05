// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.for_ObserverDelivery.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.given;

public class materialized_reactors_with_a_delivery_hook : Specification
{
    protected readonly Subject<IEnumerable<Item>> _instances = new();
    protected readonly List<string> _log = [];
    protected readonly TaskCompletionSource _invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _left = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ReadModelReactors _reactors;
    ServiceProvider _services;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        eventStore.Name.Returns((EventStoreName)"store");
        eventStore.Namespace.Returns((EventStoreNamespaceName)"tenant-x");
        var materialized = Substitute.For<IMaterializedReadModels>();
        eventStore.ReadModels.Materialized.Returns(materialized);
        materialized.ObserveInstances<Item>(Arg.Any<InstanceCountToSkip>(), Arg.Any<InstanceCount>()).Returns(_ => _instances.AsObservable());
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.ReadModelReactors.Returns([typeof(Reactor)]);
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.Activate(Arg.Any<IServiceProvider>(), typeof(Reactor)).Returns(_ =>
        {
            _log.Add($"activated:{ObserverDeliveryContext.Current?.Namespace}");
            return new ActivatedArtifact(new Reactor(), typeof(Reactor), Substitute.For<ILogger<ActivatedArtifact>>());
        });
        var invoker = Substitute.For<IReadModelReactorInvoker>();
        invoker.Invoke(eventStore, Arg.Any<object>(), Arg.Any<ReadModelReactorMethod>(), Arg.Any<object>(), Arg.Any<EventContext>(), Arg.Any<IServiceProvider>())
            .Returns(_ =>
            {
                _invoked.TrySetResult();
                return Task.CompletedTask;
            });
        _services = new ServiceCollection()
            .AddSingleton<IObserverDeliveryScope>(new a_recording_delivery_scope(_log, () => _left.TrySetResult()))
            .BuildServiceProvider();
        _reactors = new ReadModelReactors(eventStore, artifacts, activator, _services, invoker, new JsonSerializerOptions(), Substitute.For<ILogger<ReadModelReactors>>());
        _reactors.Start();
        _instances.OnNext([new Item("1", "original")]);
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
