// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Setup.for_ChronicleInitializer.when_initializing;

public class and_there_is_an_event_store : given.a_chronicle_initializer
{
    static readonly EventStoreName _eventStore = "my-event-store";

    INamespaces _eventStoreNamespacesGrain;
    IReadModelsManager _readModelsManager;
    IProjectionsManager _projectionsManager;
    IWebhooks _webhooks;

    void Establish()
    {
        _storage.GetEventStores()
            .Returns(Task.FromResult<IEnumerable<EventStoreName>>([_eventStore]));

        _eventStoreNamespacesGrain = Substitute.For<INamespaces>();
        _eventStoreNamespacesGrain.GetAll()
            .Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>(
                [EventStoreNamespaceName.Default]));

        _readModelsManager = Substitute.For<IReadModelsManager>();
        _projectionsManager = Substitute.For<IProjectionsManager>();
        _webhooks = Substitute.For<IWebhooks>();

        _projectionsManager.GetProjectionDefinitions()
            .Returns(Task.FromResult<IEnumerable<ProjectionDefinition>>([]));

        _grainFactory.GetGrain<INamespaces>(_eventStore.Value).Returns(_eventStoreNamespacesGrain);
        _grainFactory.GetGrain<IReadModelsManager>(_eventStore.Value).Returns(_readModelsManager);
        _grainFactory.GetGrain<IProjectionsManager>(_eventStore.Value).Returns(_projectionsManager);
        _grainFactory.GetGrain<IWebhooks>(_eventStore.Value).Returns(_webhooks);
    }

    async Task Because() => await _initializer.Initialize();

    [Fact] void should_discover_event_types_for_the_event_store() =>
        _eventTypes.Received(1).DiscoverAndRegister(_eventStore);
    [Fact] void should_ensure_default_namespace_for_the_event_store() =>
        _eventStoreNamespacesGrain.Received(1).EnsureDefault();
    [Fact] void should_ensure_read_models_manager() =>
        _readModelsManager.Received(1).Ensure();
    [Fact] void should_ensure_projections_manager() =>
        _projectionsManager.Received(1).Ensure();
    [Fact] void should_ensure_webhooks_manager() =>
        _webhooks.Received(1).Ensure();
    [Fact] void should_register_projection_definitions_with_the_service_client() =>
        _projectionsServiceClient.Received(1).Register(
            _eventStore,
            Arg.Any<IEnumerable<ProjectionDefinition>>());
    [Fact] void should_discover_and_register_reactors_for_default_namespace() =>
        _reactors.Received(1).DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default);
}
