// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_tag_metadata;

public class and_only_tags_have_changed : Specification
{
    Projection _grain;
    TestKitSilo _silo;
    IStorage<ProjectionDefinition> _stateStorage;
    ProjectionDefinition _incoming;
    INamespaces _namespaces;
    ProjectionDefinition _persisted;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        var existing = for_ProjectionsManager.given.a_projections_manager_grain.CreateDefinition("the-projection", "the-read-model") with
        {
            Tags = ["Orders"],
            LastUpdated = DateTimeOffset.Parse("2026-01-01T00:00:00Z")
        };
        _incoming = existing with { Tags = ["Analytics"] };
        _stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        _stateStorage.State = existing;
        _silo.Options.StorageFactory = _ => _stateStorage;

        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Projections.Has(Arg.Any<ProjectionId>()).Returns(true);
        storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.GetAll().Returns([]);
        var objectComparer = new ObjectComparer();
        _silo.AddService<Storage.IStorage>(storage);
        _silo.AddService<IObjectComparer>(objectComparer);
        _silo.AddService<IProjectionDefinitionComparer>(new ProjectionDefinitionComparer(storage, objectComparer, NullLogger<ProjectionDefinitionComparer>.Instance));
        _silo.AddService(Substitute.For<IProjectionFactory>());
        _silo.AddService(Options.Create(new ChronicleOptions()));
        _namespaces = Substitute.For<INamespaces>();
        _namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => _namespaces);
        _grain = await _silo.CreateGrainAsync<Projection>(new ProjectionKey("the-projection", "the-event-store").ToString());
    }

    async Task Because()
    {
        await _grain.SetDefinition(_incoming);
        _persisted = await _grain.GetDefinition();
    }

    [Fact] void should_keep_the_new_tags() => _persisted.Tags!.ShouldContainOnly("Analytics");
    [Fact] async Task should_write_the_projection_state() => await _stateStorage.Received(1).WriteStateAsync();
    [Fact] void should_keep_last_updated() => _persisted.LastUpdated.ShouldEqual(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    [Fact] void should_not_schedule_a_replay() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] async Task should_not_plan_namespace_replays_or_recommendations() => await _namespaces.DidNotReceive().GetAll();
}
