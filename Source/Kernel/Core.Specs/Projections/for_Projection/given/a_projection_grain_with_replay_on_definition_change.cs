// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class a_projection_grain_with_replay_on_definition_change : Specification
{
    protected Projection _grain;
    protected TestKitSilo _silo;
    protected ProjectionDefinition _definition;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var definitionComparer = Substitute.For<IProjectionDefinitionComparer>();
        definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Different);
        _silo.AddService(definitionComparer);

        _silo.AddService(Substitute.For<IProjectionFactory>());
        _silo.AddService(Substitute.For<IObjectComparer>());

        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.GetAll().Returns([]);
        _silo.AddService(storage);

        _silo.AddService(Options.Create(new ChronicleOptions
        {
            Observers = new Observers { ReplayOnDefinitionChange = true }
        }));

        var namespacesGrain = Substitute.For<INamespaces>();
        namespacesGrain.GetAll().Returns([]);
        _silo.AddProbe(_ => namespacesGrain);

        _definition = CreateDefinition("the-read-model");

        // ProjectionDefinition has no parameterless constructor, so the test silo cannot materialize
        // an initial state on its own. Seed the storage with the definition the grain starts out with.
        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = CreateDefinition("the-previous-read-model");
        _silo.Options.StorageFactory = _ => stateStorage;

        _grain = await _silo.CreateGrainAsync<Projection>(new ProjectionKey("the-projection", "the-event-store").ToString());
    }

    static ProjectionDefinition CreateDefinition(string readModel) => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        "the-projection",
        readModel,
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
}
