// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.for_ObserverDelivery.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.given;

/// <summary>
/// A dispatcher over a real container, where the event store is resolved the way a host resolves it and the
/// namespace resolver answers another namespace than the one the events are delivered for.
/// </summary>
public class a_reactor_dispatcher_in_a_host : all_dependencies
{
    internal ReactorDispatcher _dispatcher;
    protected ReactorHandler _handler;
    protected List<string> _log;
    ServiceProvider _hostProvider;

    void Establish()
    {
        _log = [];
        var resolved = (EventStoreNamespaceName)"namespace-y";

        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(call =>
        {
            var store = Substitute.For<IEventStore>();
            store.Name.Returns(call.Arg<EventStoreName>());
            store.Namespace.Returns(call.Arg<EventStoreNamespaceName>());
            return Task.FromResult(store);
        });
        var namespaceResolver = Substitute.For<IEventStoreNamespaceResolver>();
        namespaceResolver.Resolve().Returns(resolved);

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new ChronicleClientOptions { EventStore = "store" }));
        services.AddSingleton(Options.Create(new ChronicleOptions()));
        services.AddLogging();
        services.AddCratisChronicleClient();
        services.AddSingleton(client);
        services.AddSingleton(namespaceResolver);
        services.AddSingleton(_log);
        services.AddSingleton<IObserverDeliveryScope>(new a_recording_delivery_scope(_log));
        services.AddTransient<DependingOnTheEventStore>();
        _hostProvider = services.BuildServiceProvider();
        _serviceProvider = _hostProvider;

        _argumentsResolver = new ReactorMethodArgumentsResolver();
        _eventTypes.AllClrTypes.Returns(new[] { typeof(Registered) }.ToImmutableList());
        _eventTypes.GetEventTypeFor(typeof(Registered)).Returns(new EventType("registered", 1));
        _eventTypes.GetClrTypeFor(new EventTypeId("registered"), Arg.Any<EventTypeGeneration>()).Returns(typeof(Registered));
        _eventSerializer.Deserialize(Arg.Any<Type>(), Arg.Any<JsonObject>()).Returns(call => Task.FromResult(Activator.CreateInstance(call.Arg<Type>())!));

        _dispatcher = new(
            _eventStore,
            _eventTypes,
            _serviceProvider,
            new ClientArtifactsActivator(_serviceProvider, NullLoggerFactory.Instance),
            _middlewaresActivator,
            _eventSerializer,
            _activitySource,
            _sideEffectHandlers,
            _reactorContextValuesBuilder,
            _argumentsResolver,
            _logger,
            LoggerFactory);
        _handler = new(_eventStore, "reactor", typeof(DependingOnTheEventStore), EventSequenceId.Log, [new("registered", 1)], _causationManager, _identityProvider);
    }

    void Destroy()
    {
        _handler.Dispose();
        _hostProvider.Dispose();
    }

    protected static string Delivered => "namespace-x";

    public record Registered;

    public class DependingOnTheEventStore : IReactor, ICanBeNotifiedWhenReplay
    {
        readonly List<string> _log;

        public DependingOnTheEventStore(IEventStore eventStore, List<string> log)
        {
            _log = log;
            log.Add("constructed");
            log.Add($"namespace:{eventStore.Namespace}");
        }

        public Task BeginReplay() => Task.CompletedTask;

        public Task EndReplay() => Task.CompletedTask;

        public Task Handle(Registered @event, EventContext context)
        {
            _log.Add("handled");
            return Task.CompletedTask;
        }
    }
}
