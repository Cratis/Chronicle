// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider;

/// <summary>
/// A freshly ensured namespace has no stored state for any observer, so every observer reads the same empty answer
/// from storage. Each one must still catch up on its own: a reactor and a reducer registering their partitions, and one
/// of them concluding its catch-up, must leave every other observer's partition sets - and what is written back for
/// it - exactly as that observer left them (#4548, #4558).
/// </summary>
public class when_several_observers_catch_up_in_a_fresh_namespace : given.the_provider
{
    static readonly Key _reactorPartition = "reactor-partition";
    static readonly Key _reducerPartition = "reducer-partition";

    readonly List<ObserverState> _saved = [];
    IGrainState<ObserverState> _reactorState;
    IGrainState<ObserverState> _reducerState;
    IGrainState<ObserverState> _projectionState;

    void Establish()
    {
        // The same instance for every observer: what any storage answers for an observer it has never stored.
        var nothingStored = new ObserverState();
        observerStateStorage.Get(Arg.Any<ObserverId>()).Returns(Task.FromResult(nothingStored));
        observerStateStorage.Save(Arg.Do<ObserverState>(state => _saved.Add(state with
        {
            CatchingUpPartitions = new HashSet<Key>(state.CatchingUpPartitions)
        })));

        _reactorState = new GrainState<ObserverState> { State = new() };
        _reducerState = new GrainState<ObserverState> { State = new() };
        _projectionState = new GrainState<ObserverState> { State = new() };
    }

    async Task Because()
    {
        await provider.ReadStateAsync("name", GrainIdFor("reactor"), _reactorState);
        await provider.ReadStateAsync("name", GrainIdFor("reducer"), _reducerState);
        await provider.ReadStateAsync("name", GrainIdFor("projection"), _projectionState);

        _reactorState.State.CatchingUpPartitions.Add(_reactorPartition);
        await provider.WriteStateAsync("name", GrainIdFor("reactor"), _reactorState);
        _reducerState.State.CatchingUpPartitions.Add(_reducerPartition);
        await provider.WriteStateAsync("name", GrainIdFor("reducer"), _reducerState);

        // The reactor's catch-up concludes and routing clears its partitions.
        _reactorState.State.CatchingUpPartitions.Clear();
        await provider.WriteStateAsync("name", GrainIdFor("reactor"), _reactorState);
    }

    [Fact] void should_keep_the_reducer_catching_up_only_its_own_partition() => _reducerState.State.CatchingUpPartitions.ShouldContainOnly(_reducerPartition);
    [Fact] void should_not_hold_back_any_partition_for_the_projection() => _projectionState.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_have_written_only_the_reactor_partition_for_the_reactor() => _saved[0].CatchingUpPartitions.ShouldContainOnly(_reactorPartition);
    [Fact] void should_have_written_only_the_reducer_partition_for_the_reducer() => _saved[1].CatchingUpPartitions.ShouldContainOnly(_reducerPartition);
    [Fact] void should_have_written_the_reactor_as_caught_up() => _saved[2].CatchingUpPartitions.ShouldBeEmpty();

    static GrainId GrainIdFor(string observerId) => GrainId.Create("type", new ObserverKey(observerId, "store", "Default", "event-log").ToString());
}
