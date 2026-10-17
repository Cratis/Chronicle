// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Statistics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

public class and_legacy_statistics_storage_is_refreshed : Specification
{
    readonly List<ProjectionDefinition> _saved = [];
    readonly List<int> _timers = [];
    readonly List<INamespaces> _namespaces = [];
    readonly List<INotifyProjectionDefinitionsChanged> _subscribers = [];
    readonly List<IStorage<ProjectionDefinition>> _stateStorages = [];

    async Task Because()
    {
        foreach (var definition in KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections)
        {
            var silo = new TestKitSilo();
            var storage = Substitute.For<Storage.IStorage>();
            storage.GetEventStore(Arg.Any<EventStoreName>()).Projections.Has(definition.Identifier).Returns(true);
            storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.GetAll().Returns([]);
            var objectComparer = new ObjectComparer();
            silo.AddService(storage);
            silo.AddService<IObjectComparer>(objectComparer);
            silo.AddService<IProjectionDefinitionComparer>(new ProjectionDefinitionComparer(storage, objectComparer, NullLogger<ProjectionDefinitionComparer>.Instance));
            silo.AddService(Substitute.For<IProjectionFactory>());
            silo.AddService(Options.Create(new ChronicleOptions
            {
                Observers = new Observers { DefinitionEvolution = DefinitionEvolutionPolicy.Automatic }
            }));
            var namespaces = Substitute.For<INamespaces>();
            namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
            silo.AddProbe(_ => namespaces);
            var state = Substitute.For<IStorage<ProjectionDefinition>>();
            state.State = definition with
            {
                HasLegacyStorageFields = true,
                Scope = ProjectionScope.Namespaced,
                FromEvery = definition.FromEvery with { Key = WellKnownExpressions.EventSourceId, AutoMap = AutoMap.Inherit }
            };
            silo.Options.StorageFactory = _ => state;
            var grain = await silo.CreateGrainAsync<Projection>(new ProjectionKey(definition.Identifier, "store").ToString());
            var subscriber = Substitute.For<INotifyProjectionDefinitionsChanged>();
            await grain.SubscribeDefinitionsChanged(subscriber);

            await grain.SetDefinition(definition);

            _saved.Add(await grain.GetDefinition());
            _timers.Add(silo.TimerRegistry.NumberOfActiveTimers);
            _namespaces.Add(namespaces);
            _subscribers.Add(subscriber);
            _stateStorages.Add(state);
            await silo.TimerRegistry.FireAllAsync();
        }
    }

    [Fact] void should_cover_both_statistics_projections() => _saved.Count.ShouldEqual(2);
    [Fact] void should_save_the_lossless_definitions() => _saved.ShouldEachConformTo(definition => !definition.HasLegacyStorageFields);
    [Fact] void should_restore_global_routing() => _saved.Select(definition => definition.Scope).ShouldContain(ProjectionScope.Global);
    [Fact] void should_persist_each_upgrade() => _stateStorages.ShouldEachConformTo(state => state.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(state.WriteStateAsync)) == 1);
    [Fact] void should_schedule_no_replay() => _timers.ShouldEachConformTo(count => count == 0);
    [Fact] void should_not_begin_evolution_in_any_namespace() => _namespaces.ShouldEachConformTo(namespaces => !namespaces.ReceivedCalls().Any());
    [Fact] void should_refresh_active_subscribers() => _subscribers.ShouldEachConformTo(subscriber => subscriber.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(subscriber.OnProjectionDefinitionsChanged)) == 1);
}
