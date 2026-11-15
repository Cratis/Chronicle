// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.for_ObserverDelivery.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.given;

/// <summary>
/// A dispatcher over a real container, where the event store is resolved the way a host resolves it and the
/// namespace resolver answers another namespace than the one the events are delivered for.
/// </summary>
public class a_reducer_dispatcher_in_a_host : all_dependencies
{
    internal ReducerDispatcher _dispatcher;
    protected List<string> _log;
    ServiceProvider _hostProvider;

    void Establish()
    {
        _log = [];
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(call =>
        {
            var store = Substitute.For<IEventStore>();
            store.Name.Returns(call.Arg<EventStoreName>());
            store.Namespace.Returns(call.Arg<EventStoreNamespaceName>());
            return Task.FromResult(store);
        });
        var namespaceResolver = Substitute.For<IEventStoreNamespaceResolver>();
        namespaceResolver.Resolve().Returns((EventStoreNamespaceName)"namespace-y");

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new ChronicleClientOptions { EventStore = "store" }));
        services.AddSingleton(Options.Create(new ChronicleOptions()));
        services.AddLogging();
        services.AddCratisChronicleClient();
        services.AddSingleton(client);
        services.AddSingleton(namespaceResolver);
        services.AddSingleton<IObserverDeliveryScope>(new a_recording_delivery_scope(_log));
        _hostProvider = services.BuildServiceProvider();
        _serviceProvider = _hostProvider;

        // What constructor injection does for a reducer: activate from the scoped provider the invoker is given.
        _handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>()).Returns(call =>
        {
            var store = call.Arg<IServiceProvider>().GetRequiredService<IEventStore>();
            _log.Add($"namespace:{store.Namespace}");
            return new ReduceResult(new State("after"), 1, [], string.Empty);
        });
        _artifactActivator.Activate(Arg.Any<IServiceProvider>(), typeof(object)).Returns(call =>
        {
            var store = call.Arg<IServiceProvider>().GetRequiredService<IEventStore>();
            _log.Add($"namespace:{store.Namespace}");
            return new ActivatedArtifact(new object(), typeof(object), Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivatedArtifact>>());
        });
        _handler.Invoker.Returns(Substitute.For<IReducerInvoker>());

        _dispatcher = new(
            _eventStore,
            _serviceProvider,
            _artifactActivator,
            _eventTypes,
            _jsonSerializerOptions,
            _identityProvider,
            _activitySource,
            _logger);
    }

    void Destroy() => _hostProvider.Dispose();
}
