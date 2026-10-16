// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider;

public class when_reading_persisted_partition_sets : given.the_provider
{
    ObserverState _persisted;
    GrainState<ObserverState> _state;

    void Establish()
    {
        _persisted = new()
        {
            CatchingUpPartitions = new HashSet<Key> { "catching-up" },
            ReplayingPartitions = new HashSet<Key> { "replaying" },
            InFlightPartitions = new HashSet<Key> { "in-flight" }
        };
        observerStateStorage.Get(Arg.Any<ObserverId>()).Returns(_persisted);
        _state = new() { State = new() };
    }

    async Task Because()
    {
        await provider.ReadStateAsync("state", GrainId.Create("observer", "observer#store#tenant#event-log"), _state);
        _persisted.CatchingUpPartitions.Clear();
        _persisted.ReplayingPartitions.Clear();
        _persisted.InFlightPartitions.Clear();
    }

    [Fact] void should_preserve_loaded_catchup_partitions() => _state.State.CatchingUpPartitions.ShouldContainOnly((Key)"catching-up");
    [Fact] void should_preserve_loaded_replay_partitions() => _state.State.ReplayingPartitions.ShouldContainOnly((Key)"replaying");
    [Fact] void should_preserve_loaded_in_flight_partitions() => _state.State.InFlightPartitions.ShouldContainOnly((Key)"in-flight");
}
