// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider;

public class when_reading_missing_state : given.the_provider
{
    readonly GrainId _grainId = GrainId.Create("type", "key");
    readonly IGrainState<ObserverState> _state = new GrainState<ObserverState> { State = new(), RecordExists = true };

    void Establish() => observerStateStorage.Get(Arg.Any<ObserverId>()).Returns(ObserverState.Empty);

    async Task Because() => await provider.ReadStateAsync("name", _grainId, _state);

    [Fact] void should_report_that_no_record_exists() => _state.RecordExists.ShouldBeFalse();
    [Fact] void should_preserve_the_missing_record_identifier() => _state.State.Identifier.ShouldEqual(ObserverId.Unspecified);
    [Fact] void should_load_default_running_state() => _state.State.RunningState.ShouldEqual(ObserverRunningState.Unknown);
}
