// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

/// <summary>
/// Reproduces #3846 - a passive read model's projection definition is set lazily from the first observed
/// event of its kind, so a query for a key can arrive before that has ever happened. Duplicates
/// given.an_immediate_projection's setup rather than inheriting from it, because TestKitSilo allows only
/// one grain per test silo and this spec needs the storage state to already carry a null ReadModel at the
/// point the grain is created.
/// </summary>
public class and_definition_has_not_been_set_yet : Specification
{
    const string EventStore = "the-event-store";
    const string Projection = "the-projection";
    const string ReadModelKey = "the-read-model-key";

    ImmediateProjection _grain;
    TestKitSilo _silo;
    ProjectionResult _result;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        var projection = Substitute.For<IProjection>();
        projection.SubscribeDefinitionsChanged(Arg.Any<INotifyProjectionDefinitionsChanged>()).Returns(Task.CompletedTask);

        var storage = Substitute.For<Storage.IStorage>();
        var eventStoreStorage = Substitute.For<Storage.IEventStoreStorage>();
        var namespaceStorage = Substitute.For<Storage.IEventStoreNamespaceStorage>();
        storage.GetEventStore(EventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(EventStoreNamespaceName.Default).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(Substitute.For<IEventSequenceStorage>());

        var logger = Substitute.For<ILogger<ImmediateProjection>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _silo.AddService(storage);
        _silo.AddService(Substitute.For<IExpandoObjectConverter>());
        _silo.AddService(logger);
        _silo.AddProbe(_ => projection);

        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            Projection,
            null!,
            true,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
        _silo.Options.StorageFactory = _ => stateStorage;

        var key = new ImmediateProjectionKey(Projection, EventStore, EventStoreNamespaceName.Default, EventSequenceId.Log, ReadModelKey);
        _grain = await _silo.CreateGrainAsync<given.TestableImmediateProjection>(key.ToString());
    }

    async Task Because() => _result = await _grain.GetModelInstance();

    [Fact] void should_not_have_a_read_model() => _result.HasReadModel.ShouldBeFalse();
}
