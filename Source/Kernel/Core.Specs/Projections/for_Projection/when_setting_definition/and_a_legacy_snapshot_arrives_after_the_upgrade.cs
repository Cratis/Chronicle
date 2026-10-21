// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

public class and_a_legacy_snapshot_arrives_after_the_upgrade : given.a_projection_grain_with_a_versioned_definition
{
    ProjectionDefinition _legacy;
    ProjectionDefinition _result;
    INotifyProjectionDefinitionsChanged _subscriber;

    async Task Establish()
    {
        var every = _stored.FromEvery with { Key = "CompositeKey", AutoMap = AutoMap.Disabled };
        var child = new ChildrenDefinition(
            "id",
            new Dictionary<Concepts.Events.EventType, FromDefinition>(),
            new Dictionary<Concepts.Events.EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            every,
            new Dictionary<Concepts.Events.EventType, RemovedWithDefinition>(),
            new Dictionary<Concepts.Events.EventType, RemovedWithJoinDefinition>());
        _stored = _stored with
        {
            Scope = ProjectionScope.Global,
            FromEvery = every,
            Children = new Dictionary<PropertyPath, ChildrenDefinition> { ["items"] = child },
            Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = child }
        };
        _stateStorage.State = _stored;
        var oldEvery = every with { Key = Concepts.WellKnownExpressions.EventSourceId, AutoMap = AutoMap.Inherit };
        var oldChild = child with { All = oldEvery, FromEvery = oldEvery };
        _legacy = _stored with
        {
            HasLegacyStorageFields = true,
            Scope = ProjectionScope.Namespaced,
            FromEvery = oldEvery,
            Children = new Dictionary<PropertyPath, ChildrenDefinition> { ["items"] = oldChild },
            Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = oldChild }
        };
        _storage.GetEventStore(Arg.Any<Concepts.EventStoreName>()).Projections.Has(_stored.Identifier).Returns(true);
        var comparer = new ProjectionDefinitionComparer(_storage, new ObjectComparer(), NullLogger<ProjectionDefinitionComparer>.Instance);
        _comparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(call => comparer.Compare(call.Arg<ProjectionKey>(), call.ArgAt<ProjectionDefinition>(1), call.ArgAt<ProjectionDefinition>(2)));
        _subscriber = Substitute.For<INotifyProjectionDefinitionsChanged>();
        await _grain.SubscribeDefinitionsChanged(_subscriber);
    }

    async Task Because()
    {
        await _grain.SetDefinition(_legacy);
        _result = await _grain.GetDefinition();
    }

    [Fact] void should_remain_lossless() => _result.HasLegacyStorageFields.ShouldBeFalse();
    [Fact] void should_keep_global_routing() => _result.Scope.ShouldEqual(ProjectionScope.Global);
    [Fact] void should_keep_the_known_every_event_key() => _result.FromEvery.Key.ShouldEqual(_stored.FromEvery.Key);
    [Fact] void should_keep_the_known_every_event_automap() => _result.FromEvery.AutoMap.ShouldEqual(AutoMap.Disabled);
    [Fact] void should_keep_child_keys_including_the_all_alias() => _result.Children.Values.ShouldEachConformTo(child => child.All.Key == _stored.FromEvery.Key && child.FromEvery.Key == child.All.Key && child.All.AutoMap == AutoMap.Disabled);
    [Fact] void should_keep_nested_keys_including_the_all_alias() => _result.Nested!.Values.ShouldEachConformTo(child => child.All.Key == _stored.FromEvery.Key && child.FromEvery.Key == child.All.Key && child.All.AutoMap == AutoMap.Disabled);
    [Fact] void should_not_replay() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] void should_not_rebuild_subscribers_with_legacy_routing() => _subscriber.ReceivedCalls().ShouldBeEmpty();
}
