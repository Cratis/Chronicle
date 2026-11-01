// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadBindings.given;

public class a_read_binding_resolver : Specification
{
    protected static readonly EventStoreName EventStore = "test-store";
    protected static readonly EventStoreNamespaceName Namespace = "test-namespace";
    protected ISinks _sinks;
    protected ISink _sink;
    protected ReadModelDefinition _definition;
    protected IReadModelInstanceVisibilities _visibilities;
    protected ReadModelReadBindings _resolver;
    protected ReadModelReadBinding _binding;

    void Establish()
    {
        _definition = new(
            "model",
            "container",
            "Model",
            ReadModelOwner.None,
            ReadModelSource.Unknown,
            ReadModelObserverType.Projection,
            "observer",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory),
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);
        _sink = Substitute.For<ISink>();
        _sinks = Substitute.For<ISinks>();
        _sinks.GetFor(_definition, _definition.ContainerName).Returns(_sink);
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore(EventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(Namespace).Returns(namespaceStorage);
        namespaceStorage.Sinks.Returns(_sinks);
        _visibilities = Substitute.For<IReadModelInstanceVisibilities>();
        _visibilities.GetFor(EventStore, Namespace, _definition).Returns(ReadModelInstanceVisibility.InitializedOnly);
        _resolver = new(storage, _visibilities);
    }
}
