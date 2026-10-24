// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

public class and_another_silo_cached_the_legacy_definition : given.a_projection_grain_with_a_versioned_definition
{
    ProjectionDefinition _remote;
    ProjectionDefinition _persisted;
    CachingProjectionDefinitionsStorage _secondSilo;
    Observation.IObserver _observer;
    int _replayTimers;

    async Task Establish()
    {
        _stored = _stored with { Scope = ProjectionScope.Global, FromEvery = _stored.FromEvery with { Key = "CompositeKey", AutoMap = AutoMap.Disabled } };
        _stateStorage.State = _stored;
        _persisted = _stored with
        {
            HasLegacyStorageFields = true,
            Scope = ProjectionScope.Namespaced,
            FromEvery = _stored.FromEvery with { Key = WellKnownExpressions.EventSourceId, AutoMap = AutoMap.Inherit }
        };
        var shared = Substitute.For<IProjectionDefinitionsStorage>();
        shared.Get(_stored.Identifier).Returns(_ => _persisted);
        shared.Has(_stored.Identifier).Returns(true);
        shared.Save(Arg.Any<ProjectionDefinition>()).Returns(call =>
        {
            _persisted = call.Arg<ProjectionDefinition>();
            return Task.CompletedTask;
        });
        var firstSilo = new CachingProjectionDefinitionsStorage(shared);
        _secondSilo = new CachingProjectionDefinitionsStorage(shared);
        await _secondSilo.Get(_stored.Identifier);
        await firstSilo.Save(_stored);
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Projections.Returns(_secondSilo);
        var comparer = new ProjectionDefinitionComparer(_storage, new ObjectComparer(), NullLogger<ProjectionDefinitionComparer>.Instance);
        _comparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(call => comparer.Compare(call.Arg<ProjectionKey>(), call.ArgAt<ProjectionDefinition>(1), call.ArgAt<ProjectionDefinition>(2)));
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => namespaces);
        _observer = Substitute.For<Observation.IObserver>();
        _observer.GetDefinition().Returns(Storage.Observation.ObserverDefinition.Empty);
        _silo.AddProbe(_ => _observer);
    }

    async Task Because()
    {
        _remote = await _secondSilo.Get(_stored.Identifier);
        _stateStorage.State = _remote;
        await _grain.SetDefinition(_stored with { Scope = ProjectionScope.Namespaced });
        _replayTimers = _silo.TimerRegistry.NumberOfActiveTimers;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_observe_the_other_silos_lossless_upgrade() => _remote.HasLegacyStorageFields.ShouldBeFalse();
    [Fact] void should_observe_the_authoritative_scope() => _remote.Scope.ShouldEqual(ProjectionScope.Global);
    [Fact] void should_observe_the_authoritative_every_event_key() => _remote.FromEvery.Key.ShouldEqual(_stored.FromEvery.Key);
    [Fact] void should_plan_replay_for_a_subsequent_real_scope_change() => _replayTimers.ShouldEqual(1);
    [Fact] async Task should_replay_the_subsequent_real_scope_change() => await _observer.Received(1).Replay();
}
