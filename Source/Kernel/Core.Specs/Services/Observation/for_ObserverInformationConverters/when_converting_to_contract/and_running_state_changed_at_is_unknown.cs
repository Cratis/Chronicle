// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_ObserverInformationConverters.when_converting_to_contract;

public class and_running_state_changed_at_is_unknown : Specification
{
    Contracts.Observation.ObserverInformation _result;

    void Because() => _result = new ObserverDefinition().ToContract(new ObserverState());

    [Fact] void should_leave_the_timestamp_unknown() => _result.RunningStateChangedAt.ShouldBeNull();
}
