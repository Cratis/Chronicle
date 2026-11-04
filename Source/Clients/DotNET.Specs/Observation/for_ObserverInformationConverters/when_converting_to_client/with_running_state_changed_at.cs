// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverInformationConverters.when_converting_to_client;

public class with_running_state_changed_at : Specification
{
    readonly DateTimeOffset _timestamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    ObserverInformation _result;

    void Because() => _result = new Contracts.Observation.ObserverInformation { Id = "observer", EventSequenceId = "event-log", RunningStateChangedAt = _timestamp }.ToClient();

    [Fact] void should_map_the_timestamp() => _result.RunningStateChangedAt.ShouldEqual(_timestamp);
}
