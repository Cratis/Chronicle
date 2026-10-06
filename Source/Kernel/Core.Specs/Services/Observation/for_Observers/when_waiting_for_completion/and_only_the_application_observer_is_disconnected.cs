// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_only_the_application_observer_is_disconnected : given.application_and_system_observers
{
    WaitForObserverCompletionResponse _result;

    async Task Because() => _result = await _observers.WaitForCompletion(_request);

    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_time_out() => _result.TimedOut.ShouldBeTrue();
    [Fact] void should_wait_only_for_the_application_observer() => _result.OutstandingObservers.ShouldContainOnly(ApplicationObserverId);
}
