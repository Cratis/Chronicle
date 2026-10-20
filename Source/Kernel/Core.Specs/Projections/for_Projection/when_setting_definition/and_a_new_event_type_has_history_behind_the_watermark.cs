// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

public class and_a_new_event_type_has_history_behind_the_watermark : given.a_projection_adding_historical_event_mappings
{
    ProjectionDefinition _result;
    int _replayTimers;

    async Task Because()
    {
        await _grain.SetDefinition(_candidate);
        _result = await _grain.GetDefinition();
        _replayTimers = _silo.TimerRegistry.NumberOfActiveTimers;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_apply_the_new_definition() => _result.ShouldBeSame(_candidate);
    [Fact] async Task should_persist_the_new_definition() => await _stateStorage.Received(1).WriteStateAsync();
    [Fact] async Task should_notify_subscribers_of_the_new_definition() => await _subscriber.Received(1).OnProjectionDefinitionsChanged(_candidate);
    [Fact] void should_schedule_one_full_replay() => _replayTimers.ShouldEqual(1);
    [Fact] async Task should_replay_the_entire_projection() => await _observer.Received(1).Replay();
    [Fact] async Task should_not_replay_any_partition_into_the_live_container() => await _observer.DidNotReceive().ReplayPartition(Arg.Any<Key>(), Arg.Any<IEnumerable<EventType>>());
}
