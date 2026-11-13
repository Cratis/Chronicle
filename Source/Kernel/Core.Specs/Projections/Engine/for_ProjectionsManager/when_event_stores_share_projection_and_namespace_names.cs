// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager;

public class when_event_stores_share_projection_and_namespace_names : given.a_manager_with_shared_definitions
{
    static readonly EventStoreName _otherStore = "other-store";
    ReadModelDefinition _otherReadModel;
    IProjection _first;
    IProjection _other;

    void Establish()
    {
        _otherReadModel = _firstReadModelDefinition with { ContainerName = "other-container" };
        _storage.GetEventStore(_otherStore).Projections.Has(_firstDefinition.Identifier).Returns(true);
        _storage.GetEventStore(_otherStore).Projections.Get(_firstDefinition.Identifier).Returns(_firstDefinition);
        var readModel = Substitute.For<IReadModel>();
        readModel.GetDefinition().Returns(_otherReadModel);
        _grainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(_firstDefinition.ReadModel, _otherStore)).Returns(readModel);
    }

    async Task Because()
    {
        for (var tenant = 0; tenant < 150; tenant++)
        {
            _first = (await _manager.GetCurrent(_eventStore, $"tenant-{tenant}", _firstDefinition.Identifier))!;
            _other = (await _manager.GetCurrent(_otherStore, $"tenant-{tenant}", _firstDefinition.Identifier))!;
        }
    }

    [Fact] void should_bound_the_total_across_stores() => _manager.CachedGraphCount.ShouldEqual(ProjectionsManager.CacheCapacity);
    [Fact] void should_not_share_graphs_across_stores() => _first.ShouldNotEqual(_other);
    [Fact] void should_compile_with_the_other_stores_read_model() => _ = _projectionFactory.Received(1).Create(_otherStore, new EventStoreNamespaceName("tenant-149"), _firstDefinition, _otherReadModel, Arg.Any<IEnumerable<EventTypeSchema>>());
}
