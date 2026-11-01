// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverInformationConverters.when_converting_to_client;

public class with_an_unknown_running_state_timestamp : Specification
{
    ObserverInformation _result;

    void Because() => _result = new Contracts.Observation.ObserverInformation { Id = "observer", EventSequenceId = "event-log" }.ToClient();

    [Fact] void should_leave_the_timestamp_unknown() => _result.RunningStateChangedAt.ShouldBeNull();
}
