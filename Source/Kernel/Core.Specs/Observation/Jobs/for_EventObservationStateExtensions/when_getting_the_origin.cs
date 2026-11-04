// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_EventObservationStateExtensions;

public class when_getting_the_origin : Specification
{
    [Fact] void should_be_a_replay_for_replayed_events() => EventObservationState.Replay.ToObservationOrigin().ShouldEqual(ObservationOrigin.Replay);
    [Fact] void should_be_a_replay_when_replay_is_combined_with_other_states() => (EventObservationState.Replay | EventObservationState.Initial).ToObservationOrigin().ShouldEqual(ObservationOrigin.Replay);
    [Fact] void should_be_a_catch_up_otherwise() => EventObservationState.None.ToObservationOrigin().ShouldEqual(ObservationOrigin.CatchUp);
}
