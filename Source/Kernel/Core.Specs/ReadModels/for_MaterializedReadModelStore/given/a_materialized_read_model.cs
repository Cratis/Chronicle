// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.given;

public class a_materialized_read_model : Specification
{
    protected static readonly EventStoreName EventStore = "test-store";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "test-namespace";

    protected IStorage _storage;
    protected ISink _sink;
    protected IReadModelsCompliance _compliance;
    protected ReadModelDefinition _definition;
    protected MaterializedReadModelStore _store;

    void Establish()
    {
        _sink = Substitute.For<ISink>();
        _compliance = Substitute.For<IReadModelsCompliance>();

        var sinks = Substitute.For<ISinks>();
        sinks.GetFor(Arg.Any<ReadModelDefinition>()).Returns(_sink);

        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        namespaceStorage.Sinks.Returns(sinks);

        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);

        _storage = Substitute.For<IStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);

        _definition = new ReadModelDefinition(
            "test-read-model",
            "test-container",
            "Test Read Model",
            ReadModelOwner.None,
            ReadModelSource.Unknown,
            ReadModelObserverType.Projection,
            "test-observer",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory),
            new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, new JsonSchema() } },
            []);

        // The release pass is asserted on separately; by default it hands back what it was given.
        _compliance.Release(
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<JsonSchema>(),
                Arg.Any<ExpandoObject>())
            .Returns(callInfo => Task.FromResult(callInfo.ArgAt<ExpandoObject>(3)));

        _compliance.Release(
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<JsonSchema>(),
                Arg.Any<IEnumerable<ExpandoObject>>())
            .Returns(callInfo => Task.FromResult(callInfo.ArgAt<IEnumerable<ExpandoObject>>(3)));

        _store = new MaterializedReadModelStore(_storage, _compliance);
    }

    protected static ExpandoObject InstanceNamed(string name)
    {
        var instance = new ExpandoObject();
        ((IDictionary<string, object?>)instance)["name"] = name;
        return instance;
    }

    protected void SinkHolds(params ExpandoObject[] instances) =>
        _sink.GetInstances(Arg.Any<ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(callInfo => Task.FromResult(new ReadModelInstances(
                instances.Skip(callInfo.ArgAt<int>(1)).Take(callInfo.ArgAt<int>(2)),
                instances.Length)));
}
