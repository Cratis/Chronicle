// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_deciding_whether_a_replay_rebuilds_a_read_model;

public class and_the_observer_is_a_reducer : Specification
{
    bool _result;

    void Because() => _result = ObserverService.RebuildsReadModel(new(new("reducer", "store", "namespace", EventSequenceId.Log), ObserverType.Reducer));

    [Fact] void should_rebuild_a_read_model() => _result.ShouldBeTrue();
}
