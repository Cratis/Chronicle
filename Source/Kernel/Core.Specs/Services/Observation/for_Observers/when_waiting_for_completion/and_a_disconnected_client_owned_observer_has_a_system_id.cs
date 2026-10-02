// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_a_disconnected_client_owned_observer_has_a_system_id : given.a_client_owned_observer_with_a_system_id
{
    WaitForObserverCompletionResponse _result;

    async Task Because() => _result = await _observers.WaitForCompletion(_request);

    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_time_out() => _result.TimedOut.ShouldBeTrue();
    [Fact] void should_wait_for_the_client_owned_observer() => _result.OutstandingObservers.ShouldContainOnly(SystemObserverId);
}
