// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_ObserverInformationConverters.when_converting_to_contract;

public class and_running_state_changed_at_is_known : Specification
{
    readonly DateTimeOffset _timestamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    Contracts.Observation.ObserverInformation _result;

    void Because() => _result = new ObserverDefinition().ToContract(new ObserverState(), _timestamp);

    [Fact] void should_map_the_timestamp() => ((DateTimeOffset?)_result.RunningStateChangedAt).ShouldEqual(_timestamp);
}
